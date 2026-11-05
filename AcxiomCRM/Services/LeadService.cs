using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Common;
using AcxiomCRM.Data;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Models.Enums;
using AcxiomCRM.Models.ViewModels;

namespace AcxiomCRM.Services;

public interface ILeadService
{
    Task<PagedResult<Lead>> GetLeadsAsync(
        string userId,
        string role,
        string? searchTerm = null,
        LeadStatus? status = null,
        LeadPriority? priority = null,
        LeadSource? source = null,
        string? assigneeId = null,
        string sortBy = "DateDesc",
        int page = 1,
        int pageSize = 10);

    Task<Lead?> GetLeadByIdAsync(int leadId, string userId, string role, bool includeRelated = false);
    Task<(bool Success, string Message, Lead? Lead)> CreateLeadAsync(Lead lead, string currentUserId, string role, string currentUserName);
    Task<(bool Success, string Message)> UpdateLeadAsync(Lead lead, string currentUserId, string role, string currentUserName);
    Task<(bool Success, string Message)> ChangeStatusAsync(int leadId, LeadStatus newStatus, string currentUserId, string role, string currentUserName);
    Task<(bool Success, string Message)> AssignLeadAsync(int leadId, string targetUserId, string currentUserId, string role, string currentUserName);
    Task<(bool Success, string Message, int? CustomerId, int? OpportunityId)> ConvertLeadAsync(
        LeadConvertViewModel model,
        string currentUserId,
        string role,
        string currentUserName);
}

public class LeadService : ILeadService
{
    private readonly ApplicationDbContext _context;
    private readonly IRecordScopeService _scopeService;
    private readonly IAuditService _auditService;
    private readonly ICustomerService _customerService;

    public LeadService(
        ApplicationDbContext context,
        IRecordScopeService scopeService,
        IAuditService auditService,
        ICustomerService customerService)
    {
        _context = context;
        _scopeService = scopeService;
        _auditService = auditService;
        _customerService = customerService;
    }

    public async Task<PagedResult<Lead>> GetLeadsAsync(
        string userId,
        string role,
        string? searchTerm = null,
        LeadStatus? status = null,
        LeadPriority? priority = null,
        LeadSource? source = null,
        string? assigneeId = null,
        string sortBy = "DateDesc",
        int page = 1,
        int pageSize = 10)
    {
        var allowedUserIds = await _scopeService.GetAllowedUserIdsAsync(userId, role);
        var query = _scopeService.FilterLeads(_context.Leads.AsNoTracking().Include(l => l.AssignedUser).Include(l => l.Customer), allowedUserIds);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            query = query.Where(l =>
                l.LeadName.Contains(term) ||
                (l.Email != null && l.Email.Contains(term)) ||
                l.Phone.Contains(term) ||
                (l.CompanyName != null && l.CompanyName.Contains(term)) ||
                l.LeadCode.Contains(term));
        }

        if (status.HasValue)
        {
            query = query.Where(l => l.Status == status.Value);
        }

        if (priority.HasValue)
        {
            query = query.Where(l => l.Priority == priority.Value);
        }

        if (source.HasValue)
        {
            query = query.Where(l => l.Source == source.Value);
        }

        if (!string.IsNullOrWhiteSpace(assigneeId))
        {
            query = query.Where(l => l.AssignedTo == assigneeId);
        }

        query = sortBy switch
        {
            "NameAsc" => query.OrderBy(l => l.LeadName),
            "NameDesc" => query.OrderByDescending(l => l.LeadName),
            "ValueDesc" => query.OrderByDescending(l => l.ExpectedValue ?? 0),
            "DateAsc" => query.OrderBy(l => l.CreatedDate),
            _ => query.OrderByDescending(l => l.CreatedDate)
        };

        var totalCount = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return new PagedResult<Lead>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = pageSize
        };
    }

    public async Task<Lead?> GetLeadByIdAsync(int leadId, string userId, string role, bool includeRelated = false)
    {
        if (!await _scopeService.CanAccessLeadAsync(leadId, userId, role))
        {
            return null;
        }

        var query = _context.Leads.AsQueryable();

        if (includeRelated)
        {
            query = query
                .Include(l => l.AssignedUser)
                .Include(l => l.Customer)
                .Include(l => l.Opportunities)
                .Include(l => l.FollowUps)
                .Include(l => l.Activities);
        }
        else
        {
            query = query.Include(l => l.AssignedUser).Include(l => l.Customer);
        }

        return await query.FirstOrDefaultAsync(l => l.LeadId == leadId);
    }

    public async Task<(bool Success, string Message, Lead? Lead)> CreateLeadAsync(
        Lead lead,
        string currentUserId,
        string role,
        string currentUserName)
    {
        if (string.IsNullOrWhiteSpace(lead.LeadName))
            return (false, "Lead Name is required.", null);

        var normPhone = PhoneUtils.NormalizePhone(lead.Phone);
        if (string.IsNullOrWhiteSpace(normPhone) || !PhoneUtils.IsValidIndianPhone(normPhone))
            return (false, "Enter a valid 10-digit Indian phone number.", null);

        string? normEmail = null;
        if (!string.IsNullOrWhiteSpace(lead.Email))
        {
            normEmail = PhoneUtils.NormalizeEmail(lead.Email);
        }

        if (lead.ExpectedValue.HasValue && lead.ExpectedValue.Value < 0)
            return (false, "Expected Value cannot be negative.", null);

        if (string.IsNullOrWhiteSpace(lead.AssignedTo))
        {
            lead.AssignedTo = currentUserId;
        }
        else if (!await _scopeService.CanAssignToAsync(lead.AssignedTo, currentUserId, role))
        {
            return (false, "You are not authorized to assign this lead to the specified user.", null);
        }

        var maxId = (await _context.Leads.MaxAsync(l => (int?)l.LeadId)) ?? 0;
        lead.LeadCode = $"LEAD-{(maxId + 1):D4}";
        lead.Phone = normPhone;
        lead.Email = normEmail;
        lead.CreatedDate = DateTime.UtcNow;
        lead.CreatedBy = currentUserName;

        _context.Leads.Add(lead);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            currentUserId,
            currentUserName,
            "Lead Created",
            "Lead",
            lead.LeadId.ToString(),
            null,
            $"Code: {lead.LeadCode}, Name: {lead.LeadName}, Status: {lead.Status}, AssignedTo: {lead.AssignedTo}",
            "Success");

        return (true, "Lead created successfully.", lead);
    }

    public async Task<(bool Success, string Message)> UpdateLeadAsync(
        Lead lead,
        string currentUserId,
        string role,
        string currentUserName)
    {
        var existing = await _context.Leads.FindAsync(lead.LeadId);
        if (existing == null)
            return (false, "Lead not found.");

        if (!await _scopeService.CanAccessLeadAsync(existing.LeadId, currentUserId, role))
            return (false, "You do not have permission to modify this lead.");

        if (existing.Status == LeadStatus.Converted && lead.Status != LeadStatus.Converted)
            return (false, "A Converted lead status cannot be reverted.");

        var normPhone = PhoneUtils.NormalizePhone(lead.Phone);
        if (!PhoneUtils.IsValidIndianPhone(normPhone))
            return (false, "Enter a valid 10-digit Indian phone number.");

        string? normEmail = null;
        if (!string.IsNullOrWhiteSpace(lead.Email))
        {
            normEmail = PhoneUtils.NormalizeEmail(lead.Email);
        }

        if (existing.AssignedTo != lead.AssignedTo && !await _scopeService.CanAssignToAsync(lead.AssignedTo, currentUserId, role))
            return (false, "You are not authorized to assign this lead to the specified user.");

        var oldValue = $"Name: {existing.LeadName}, Status: {existing.Status}, Priority: {existing.Priority}, Assigned: {existing.AssignedTo}";

        existing.LeadName = lead.LeadName;
        existing.Email = normEmail;
        existing.Phone = normPhone;
        existing.CompanyName = lead.CompanyName;
        existing.Source = lead.Source;
        existing.Priority = lead.Priority;
        existing.ExpectedValue = lead.ExpectedValue;
        existing.Notes = lead.Notes;
        existing.AssignedTo = lead.AssignedTo;
        existing.ModifiedDate = DateTime.UtcNow;
        existing.ModifiedBy = currentUserName;

        // Status transition validation
        if (existing.Status != lead.Status)
        {
            var canTransition = ValidateStatusTransition(existing.Status, lead.Status);
            if (!canTransition)
                return (false, $"Invalid status transition from {existing.Status} to {lead.Status}.");

            existing.Status = lead.Status;
        }

        await _context.SaveChangesAsync();

        var newValue = $"Name: {existing.LeadName}, Status: {existing.Status}, Priority: {existing.Priority}, Assigned: {existing.AssignedTo}";

        await _auditService.LogAsync(
            currentUserId,
            currentUserName,
            "Lead Updated",
            "Lead",
            existing.LeadId.ToString(),
            oldValue,
            newValue,
            "Success");

        return (true, "Lead updated successfully.");
    }

    public async Task<(bool Success, string Message)> ChangeStatusAsync(
        int leadId,
        LeadStatus newStatus,
        string currentUserId,
        string role,
        string currentUserName)
    {
        var existing = await _context.Leads.FindAsync(leadId);
        if (existing == null)
            return (false, "Lead not found.");

        if (!await _scopeService.CanAccessLeadAsync(leadId, currentUserId, role))
            return (false, "You do not have permission to update this lead.");

        if (existing.Status == LeadStatus.Converted)
            return (false, "Converted leads cannot change status.");

        if (!ValidateStatusTransition(existing.Status, newStatus))
            return (false, $"Invalid status transition from {existing.Status} to {newStatus}.");

        var oldStatus = existing.Status;
        existing.Status = newStatus;
        existing.ModifiedDate = DateTime.UtcNow;
        existing.ModifiedBy = currentUserName;

        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            currentUserId,
            currentUserName,
            "Lead Status Changed",
            "Lead",
            leadId.ToString(),
            $"Status: {oldStatus}",
            $"Status: {newStatus}",
            "Success");

        return (true, $"Lead status changed to {newStatus}.");
    }

    public async Task<(bool Success, string Message)> AssignLeadAsync(
        int leadId,
        string targetUserId,
        string currentUserId,
        string role,
        string currentUserName)
    {
        var existing = await _context.Leads.FindAsync(leadId);
        if (existing == null)
            return (false, "Lead not found.");

        if (!await _scopeService.CanAccessLeadAsync(leadId, currentUserId, role))
            return (false, "You do not have permission to manage this lead.");

        if (!await _scopeService.CanAssignToAsync(targetUserId, currentUserId, role))
            return (false, "You are not authorized to assign this lead to the selected user.");

        var oldAssignee = existing.AssignedTo;
        existing.AssignedTo = targetUserId;
        existing.ModifiedDate = DateTime.UtcNow;
        existing.ModifiedBy = currentUserName;

        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            currentUserId,
            currentUserName,
            "Lead Assigned",
            "Lead",
            leadId.ToString(),
            $"AssignedTo: {oldAssignee}",
            $"AssignedTo: {targetUserId}",
            "Success");

        return (true, "Lead successfully assigned.");
    }

    public async Task<(bool Success, string Message, int? CustomerId, int? OpportunityId)> ConvertLeadAsync(
        LeadConvertViewModel model,
        string currentUserId,
        string role,
        string currentUserName)
    {
        var lead = await _context.Leads.FindAsync(model.LeadId);
        if (lead == null)
            return (false, "Lead not found.", null, null);

        if (!await _scopeService.CanAccessLeadAsync(lead.LeadId, currentUserId, role))
            return (false, "You do not have permission to convert this lead.", null, null);

        if (lead.Status == LeadStatus.Converted)
            return (false, "This lead has already been converted and cannot be converted again.", null, null);

        var normEmail = PhoneUtils.NormalizeEmail(model.Email);
        var normPhone = PhoneUtils.NormalizePhone(model.Phone);

        if (string.IsNullOrWhiteSpace(normEmail))
            return (false, "Customer Email is required.", null, null);

        if (!PhoneUtils.IsValidIndianPhone(normPhone))
            return (false, "Enter a valid 10-digit Indian phone number.", null, null);

        // Check if customer with email or phone already exists
        if (!await _customerService.IsEmailUniqueAsync(normEmail))
            return (false, "A customer with this email address already exists.", null, null);

        if (!await _customerService.IsPhoneUniqueAsync(normPhone))
            return (false, "A customer with this phone number already exists.", null, null);

        if (!await _scopeService.CanAssignToAsync(model.AssignedTo, currentUserId, role))
            return (false, "You are not authorized to assign to the specified user.", null, null);

        // Transactional execution of conversion
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // 1. Create Customer
            var maxCustId = (await _context.Customers.MaxAsync(c => (int?)c.CustomerId)) ?? 0;
            var customer = new Customer
            {
                CustomerCode = $"CUST-{(maxCustId + 1):D4}",
                CustomerName = model.CustomerName.Trim(),
                Email = normEmail,
                Phone = normPhone,
                CompanyName = model.CompanyName,
                OwnerId = model.AssignedTo,
                Status = CustomerStatus.Active,
                CreatedDate = DateTime.UtcNow,
                CreatedBy = currentUserName
            };

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            // 2. Optionally create Opportunity
            Opportunity? opportunity = null;
            if (model.CreateOpportunity && model.OpportunityAmount.HasValue && model.OpportunityAmount.Value > 0)
            {
                var oppName = string.IsNullOrWhiteSpace(model.OpportunityName)
                    ? $"{customer.CustomerName} - Initial Deal"
                    : model.OpportunityName.Trim();

                opportunity = new Opportunity
                {
                    OpportunityName = oppName,
                    CustomerId = customer.CustomerId,
                    LeadId = lead.LeadId,
                    Amount = model.OpportunityAmount.Value,
                    Stage = OpportunityStage.Qualification,
                    Probability = Math.Clamp(model.OpportunityProbability, 0, 100),
                    ExpectedCloseDate = model.ExpectedCloseDate ?? DateTime.Today.AddDays(30),
                    Status = OpportunityStatus.Open,
                    AssignedTo = model.AssignedTo,
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = currentUserName
                };

                _context.Opportunities.Add(opportunity);
                await _context.SaveChangesAsync();
            }

            // 3. Update Lead
            lead.Status = LeadStatus.Converted;
            lead.CustomerId = customer.CustomerId;
            lead.ModifiedDate = DateTime.UtcNow;
            lead.ModifiedBy = currentUserName;

            await _context.SaveChangesAsync();

            // Commit transaction
            await transaction.CommitAsync();

            // 4. Audit Log
            await _auditService.LogAsync(
                currentUserId,
                currentUserName,
                "Lead Converted",
                "Lead",
                lead.LeadId.ToString(),
                "Status: Qualified",
                $"Converted to CustomerId: {customer.CustomerId}" + (opportunity != null ? $", OpportunityId: {opportunity.OpportunityId}" : ""),
                "Success");

            return (true, "Lead successfully converted to Customer" + (opportunity != null ? " and Opportunity." : "."), customer.CustomerId, opportunity?.OpportunityId);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return (false, "An error occurred while converting the lead: " + ex.Message, null, null);
        }
    }

    private static bool ValidateStatusTransition(LeadStatus current, LeadStatus next)
    {
        if (current == next) return true;
        if (current == LeadStatus.Converted) return false; // Converted cannot transition

        return current switch
        {
            LeadStatus.New => next is LeadStatus.Contacted or LeadStatus.Unqualified or LeadStatus.Lost or LeadStatus.Qualified,
            LeadStatus.Contacted => next is LeadStatus.Qualified or LeadStatus.Unqualified or LeadStatus.Lost,
            LeadStatus.Qualified => next is LeadStatus.Converted or LeadStatus.Lost or LeadStatus.Unqualified or LeadStatus.Contacted,
            LeadStatus.Unqualified => next is LeadStatus.Contacted or LeadStatus.Lost,
            LeadStatus.Lost => next is LeadStatus.Contacted or LeadStatus.New,
            _ => false
        };
    }
}

