using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Common;
using AcxiomCRM.Data;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Models.Enums;
using AcxiomCRM.Models.ViewModels;

namespace AcxiomCRM.Services;

public interface ICustomerService
{
    Task<PagedResult<Customer>> GetCustomersAsync(
        string userId,
        string role,
        string? searchTerm = null,
        CustomerStatus? status = null,
        string? ownerId = null,
        string sortBy = "DateDesc",
        int page = 1,
        int pageSize = 10);

    Task<Customer?> GetCustomerByIdAsync(int customerId, string userId, string role, bool includeRelated = false);
    Task<(bool Success, string Message, Customer? Customer)> CreateCustomerAsync(Customer customer, string currentUserId, string role, string currentUserName);
    Task<(bool Success, string Message)> UpdateCustomerAsync(Customer customer, string currentUserId, string role, string currentUserName);
    Task<(bool Success, string Message)> DeactivateCustomerAsync(int customerId, string currentUserId, string role, string currentUserName);
    Task<bool> IsEmailUniqueAsync(string email, int? excludeCustomerId = null);
    Task<bool> IsPhoneUniqueAsync(string phone, int? excludeCustomerId = null);
}

public class CustomerService : ICustomerService
{
    private readonly ApplicationDbContext _context;
    private readonly IRecordScopeService _scopeService;
    private readonly IAuditService _auditService;

    public CustomerService(
        ApplicationDbContext context,
        IRecordScopeService scopeService,
        IAuditService auditService)
    {
        _context = context;
        _scopeService = scopeService;
        _auditService = auditService;
    }

    public async Task<PagedResult<Customer>> GetCustomersAsync(
        string userId,
        string role,
        string? searchTerm = null,
        CustomerStatus? status = null,
        string? ownerId = null,
        string sortBy = "DateDesc",
        int page = 1,
        int pageSize = 10)
    {
        var allowedUserIds = await _scopeService.GetAllowedUserIdsAsync(userId, role);
        var query = _scopeService.FilterCustomers(_context.Customers.AsNoTracking().Include(c => c.Owner), allowedUserIds);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            query = query.Where(c =>
                c.CustomerName.Contains(term) ||
                c.Email.Contains(term) ||
                c.Phone.Contains(term) ||
                (c.CompanyName != null && c.CompanyName.Contains(term)) ||
                c.CustomerCode.Contains(term));
        }

        if (status.HasValue)
        {
            query = query.Where(c => c.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(ownerId))
        {
            query = query.Where(c => c.OwnerId == ownerId);
        }

        query = sortBy switch
        {
            "NameAsc" => query.OrderBy(c => c.CustomerName),
            "NameDesc" => query.OrderByDescending(c => c.CustomerName),
            "DateAsc" => query.OrderBy(c => c.CreatedDate),
            _ => query.OrderByDescending(c => c.CreatedDate)
        };

        var totalCount = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return new PagedResult<Customer>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = pageSize
        };
    }

    public async Task<Customer?> GetCustomerByIdAsync(int customerId, string userId, string role, bool includeRelated = false)
    {
        if (!await _scopeService.CanAccessCustomerAsync(customerId, userId, role))
        {
            return null;
        }

        var query = _context.Customers.AsQueryable();

        if (includeRelated)
        {
            query = query
                .Include(c => c.Owner)
                .Include(c => c.Leads)
                .Include(c => c.Opportunities)
                .Include(c => c.FollowUps)
                .Include(c => c.Activities);
        }
        else
        {
            query = query.Include(c => c.Owner);
        }

        return await query.FirstOrDefaultAsync(c => c.CustomerId == customerId);
    }

    public async Task<(bool Success, string Message, Customer? Customer)> CreateCustomerAsync(
        Customer customer,
        string currentUserId,
        string role,
        string currentUserName)
    {
        if (string.IsNullOrWhiteSpace(customer.CustomerName))
            return (false, "Customer Name is required.", null);

        var normEmail = PhoneUtils.NormalizeEmail(customer.Email);
        if (string.IsNullOrWhiteSpace(normEmail))
            return (false, "Enter a valid email address.", null);

        var normPhone = PhoneUtils.NormalizePhone(customer.Phone);
        if (string.IsNullOrWhiteSpace(normPhone) || !PhoneUtils.IsValidIndianPhone(normPhone))
            return (false, "Enter a valid 10-digit Indian phone number.", null);

        // Check uniqueness
        if (!await IsEmailUniqueAsync(normEmail))
            return (false, "A customer with this email address already exists.", null);

        if (!await IsPhoneUniqueAsync(normPhone))
            return (false, "A customer with this phone number already exists.", null);

        // Verify assignment permission
        if (string.IsNullOrWhiteSpace(customer.OwnerId))
        {
            customer.OwnerId = currentUserId;
        }
        else if (!await _scopeService.CanAssignToAsync(customer.OwnerId, currentUserId, role))
        {
            return (false, "You are not authorized to assign this customer to the specified user.", null);
        }

        // Generate customer code
        var maxId = (await _context.Customers.MaxAsync(c => (int?)c.CustomerId)) ?? 0;
        customer.CustomerCode = $"CUST-{(maxId + 1):D4}";
        customer.Email = normEmail;
        customer.Phone = normPhone;
        customer.CreatedDate = DateTime.UtcNow;
        customer.CreatedBy = currentUserName;

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            currentUserId,
            currentUserName,
            "Customer Created",
            "Customer",
            customer.CustomerId.ToString(),
            null,
            $"Code: {customer.CustomerCode}, Name: {customer.CustomerName}, Email: {customer.Email}",
            "Success");

        return (true, "Customer created successfully.", customer);
    }

    public async Task<(bool Success, string Message)> UpdateCustomerAsync(
        Customer customer,
        string currentUserId,
        string role,
        string currentUserName)
    {
        var existing = await _context.Customers.FindAsync(customer.CustomerId);
        if (existing == null)
            return (false, "Customer not found.");

        if (!await _scopeService.CanAccessCustomerAsync(existing.CustomerId, currentUserId, role))
            return (false, "You do not have permission to modify this customer.");

        var normEmail = PhoneUtils.NormalizeEmail(customer.Email);
        var normPhone = PhoneUtils.NormalizePhone(customer.Phone);

        if (!PhoneUtils.IsValidIndianPhone(normPhone))
            return (false, "Enter a valid 10-digit Indian phone number.");

        if (!await IsEmailUniqueAsync(normEmail, existing.CustomerId))
            return (false, "Another customer with this email address already exists.");

        if (!await IsPhoneUniqueAsync(normPhone, existing.CustomerId))
            return (false, "Another customer with this phone number already exists.");

        if (existing.OwnerId != customer.OwnerId && !await _scopeService.CanAssignToAsync(customer.OwnerId, currentUserId, role))
            return (false, "You are not authorized to assign this customer to the specified user.");

        var oldValue = $"Name: {existing.CustomerName}, Email: {existing.Email}, Phone: {existing.Phone}, Status: {existing.Status}";

        existing.CustomerName = customer.CustomerName;
        existing.Email = normEmail;
        existing.Phone = normPhone;
        existing.CompanyName = customer.CompanyName;
        existing.Address = customer.Address;
        existing.City = customer.City;
        existing.State = customer.State;
        existing.Status = customer.Status;
        existing.OwnerId = customer.OwnerId;
        existing.ModifiedDate = DateTime.UtcNow;
        existing.ModifiedBy = currentUserName;

        await _context.SaveChangesAsync();

        var newValue = $"Name: {existing.CustomerName}, Email: {existing.Email}, Phone: {existing.Phone}, Status: {existing.Status}";

        await _auditService.LogAsync(
            currentUserId,
            currentUserName,
            "Customer Updated",
            "Customer",
            existing.CustomerId.ToString(),
            oldValue,
            newValue,
            "Success");

        return (true, "Customer updated successfully.");
    }

    public async Task<(bool Success, string Message)> DeactivateCustomerAsync(
        int customerId,
        string currentUserId,
        string role,
        string currentUserName)
    {
        var existing = await _context.Customers.FindAsync(customerId);
        if (existing == null)
            return (false, "Customer not found.");

        if (!await _scopeService.CanAccessCustomerAsync(customerId, currentUserId, role))
            return (false, "You do not have permission to deactivate this customer.");

        existing.Status = CustomerStatus.Inactive;
        existing.ModifiedDate = DateTime.UtcNow;
        existing.ModifiedBy = currentUserName;

        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            currentUserId,
            currentUserName,
            "Customer Deactivated",
            "Customer",
            customerId.ToString(),
            "Status: Active",
            "Status: Inactive",
            "Success");

        return (true, "Customer deactivated successfully.");
    }

    public async Task<bool> IsEmailUniqueAsync(string email, int? excludeCustomerId = null)
    {
        var norm = PhoneUtils.NormalizeEmail(email);
        var query = _context.Customers.AsNoTracking().Where(c => c.Email.ToLower() == norm.ToLower());
        if (excludeCustomerId.HasValue)
        {
            query = query.Where(c => c.CustomerId != excludeCustomerId.Value);
        }
        return !await query.AnyAsync();
    }

    public async Task<bool> IsPhoneUniqueAsync(string phone, int? excludeCustomerId = null)
    {
        var norm = PhoneUtils.NormalizePhone(phone);
        var query = _context.Customers.AsNoTracking().Where(c => c.Phone == norm);
        if (excludeCustomerId.HasValue)
        {
            query = query.Where(c => c.CustomerId != excludeCustomerId.Value);
        }
        return !await query.AnyAsync();
    }
}

