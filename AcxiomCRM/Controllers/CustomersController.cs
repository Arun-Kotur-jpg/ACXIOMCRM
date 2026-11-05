using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using AcxiomCRM.Authorization;
using AcxiomCRM.Data;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Models.Enums;
using AcxiomCRM.Models.ViewModels;
using AcxiomCRM.Services;

namespace AcxiomCRM.Controllers;

[Authorize]
public class CustomersController : BaseController
{
    private readonly ICustomerService _customerService;
    private readonly IUserService _userService;
    private readonly ApplicationDbContext _context;

    public CustomersController(
        ICustomerService customerService,
        IUserService userService,
        ApplicationDbContext context)
    {
        _customerService = customerService;
        _userService = userService;
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? searchTerm = null,
        CustomerStatus? status = null,
        string? ownerId = null,
        string sortBy = "DateDesc",
        int page = 1)
    {
        var result = await _customerService.GetCustomersAsync(
            CurrentUserId,
            CurrentUserRole,
            searchTerm,
            status,
            ownerId,
            sortBy,
            page,
            10);

        var activeUsers = await _userService.GetActiveUsersAsync();

        var model = new CustomerListViewModel
        {
            Customers = result,
            SearchTerm = searchTerm,
            StatusFilter = status,
            OwnerFilter = ownerId,
            SortBy = sortBy,
            AvailableOwners = new SelectList(activeUsers, "Id", "FullName", ownerId)
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var customer = await _customerService.GetCustomerByIdAsync(id, CurrentUserId, CurrentUserRole, includeRelated: true);
        if (customer == null)
        {
            return NotFound();
        }

        var auditLogs = _context.AuditLogs
            .Where(a => a.EntityName == "Customer" && a.RecordId == id.ToString())
            .OrderByDescending(a => a.CreatedDate)
            .Take(10)
            .ToList();

        var model = new CustomerDetailsViewModel
        {
            Customer = customer,
            RelatedLeads = customer.Leads.OrderByDescending(l => l.CreatedDate).ToList(),
            RelatedOpportunities = customer.Opportunities.OrderByDescending(o => o.CreatedDate).ToList(),
            RelatedFollowUps = customer.FollowUps.OrderByDescending(f => f.FollowUpDate).ToList(),
            RelatedActivities = customer.Activities.OrderByDescending(a => a.ActivityDate).ToList(),
            AuditLogs = auditLogs
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var activeUsers = await _userService.GetActiveUsersAsync();
        var model = new CustomerCreateEditViewModel
        {
            OwnerId = CurrentUserId,
            AvailableOwners = new SelectList(activeUsers, "Id", "FullName", CurrentUserId)
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CustomerCreateEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var activeUsers = await _userService.GetActiveUsersAsync();
            model.AvailableOwners = new SelectList(activeUsers, "Id", "FullName", model.OwnerId);
            return View(model);
        }

        var customer = new Customer
        {
            CustomerName = model.CustomerName.Trim(),
            Email = model.Email.Trim(),
            Phone = model.Phone.Trim(),
            CompanyName = model.CompanyName,
            Address = model.Address,
            City = model.City,
            State = model.State,
            Status = model.Status,
            OwnerId = model.OwnerId
        };

        var (success, message, created) = await _customerService.CreateCustomerAsync(
            customer,
            CurrentUserId,
            CurrentUserRole,
            CurrentUserName);

        if (!success)
        {
            ModelState.AddModelError(string.Empty, message);
            var activeUsers = await _userService.GetActiveUsersAsync();
            model.AvailableOwners = new SelectList(activeUsers, "Id", "FullName", model.OwnerId);
            return View(model);
        }

        TempData["SuccessMessage"] = message;
        return RedirectToAction(nameof(Details), new { id = created!.CustomerId });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var customer = await _customerService.GetCustomerByIdAsync(id, CurrentUserId, CurrentUserRole);
        if (customer == null)
        {
            return NotFound();
        }

        var activeUsers = await _userService.GetActiveUsersAsync();
        var model = new CustomerCreateEditViewModel
        {
            CustomerId = customer.CustomerId,
            CustomerCode = customer.CustomerCode,
            CustomerName = customer.CustomerName,
            Email = customer.Email,
            Phone = customer.Phone,
            CompanyName = customer.CompanyName,
            Address = customer.Address,
            City = customer.City,
            State = customer.State,
            Status = customer.Status,
            OwnerId = customer.OwnerId,
            AvailableOwners = new SelectList(activeUsers, "Id", "FullName", customer.OwnerId)
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CustomerCreateEditViewModel model)
    {
        if (id != model.CustomerId)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            var activeUsers = await _userService.GetActiveUsersAsync();
            model.AvailableOwners = new SelectList(activeUsers, "Id", "FullName", model.OwnerId);
            return View(model);
        }

        var customer = new Customer
        {
            CustomerId = model.CustomerId,
            CustomerName = model.CustomerName.Trim(),
            Email = model.Email.Trim(),
            Phone = model.Phone.Trim(),
            CompanyName = model.CompanyName,
            Address = model.Address,
            City = model.City,
            State = model.State,
            Status = model.Status,
            OwnerId = model.OwnerId
        };

        var (success, message) = await _customerService.UpdateCustomerAsync(
            customer,
            CurrentUserId,
            CurrentUserRole,
            CurrentUserName);

        if (!success)
        {
            ModelState.AddModelError(string.Empty, message);
            var activeUsers = await _userService.GetActiveUsersAsync();
            model.AvailableOwners = new SelectList(activeUsers, "Id", "FullName", model.OwnerId);
            return View(model);
        }

        TempData["SuccessMessage"] = message;
        return RedirectToAction(nameof(Details), new { id = model.CustomerId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(int id)
    {
        var (success, message) = await _customerService.DeactivateCustomerAsync(
            id,
            CurrentUserId,
            CurrentUserRole,
            CurrentUserName);

        if (!success)
        {
            TempData["ErrorMessage"] = message;
        }
        else
        {
            TempData["SuccessMessage"] = message;
        }

        return RedirectToAction(nameof(Index));
    }
}

