using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Models.Enums;
using AcxiomCRM.Models.ViewModels;
using AcxiomCRM.Services;

namespace AcxiomCRM.Controllers;

[Authorize]
public class FollowUpsController : BaseController
{
    private readonly IFollowUpService _followUpService;
    private readonly IUserService _userService;
    private readonly IRecordScopeService _scopeService;
    private readonly ApplicationDbContext _context;

    public FollowUpsController(
        IFollowUpService followUpService,
        IUserService userService,
        IRecordScopeService scopeService,
        ApplicationDbContext context)
    {
        _followUpService = followUpService;
        _userService = userService;
        _scopeService = scopeService;
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? searchTerm = null,
        FollowUpStatus? status = null,
        FollowUpType? type = null,
        string? assigneeId = null,
        string scope = "All",
        int page = 1)
    {
        var result = await _followUpService.GetFollowUpsAsync(
            CurrentUserId,
            CurrentUserRole,
            searchTerm,
            status,
            type,
            assigneeId,
            scope,
            page,
            10);

        var (upcoming, overdue) = await _followUpService.GetCountsAsync(CurrentUserId, CurrentUserRole);
        var activeUsers = await _userService.GetActiveUsersAsync();

        var model = new FollowUpListViewModel
        {
            FollowUps = result,
            SearchTerm = searchTerm,
            StatusFilter = status,
            TypeFilter = type,
            AssigneeFilter = assigneeId,
            FilterScope = scope,
            UpcomingCount = upcoming,
            OverdueCount = overdue,
            AvailableAssignees = new SelectList(activeUsers, "Id", "FullName", assigneeId)
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? customerId = null, int? leadId = null, int? opportunityId = null)
    {
        var model = new FollowUpCreateEditViewModel
        {
            CustomerId = customerId,
            LeadId = leadId,
            OpportunityId = opportunityId,
            FollowUpDate = DateTime.Now.AddHours(2),
            AssignedTo = CurrentUserId
        };

        await PopulateDropDownsAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FollowUpCreateEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateDropDownsAsync(model);
            return View(model);
        }

        var followUp = new FollowUp
        {
            CustomerId = model.CustomerId,
            LeadId = model.LeadId,
            OpportunityId = model.OpportunityId,
            FollowUpDate = model.FollowUpDate,
            Subject = model.Subject.Trim(),
            FollowUpType = model.FollowUpType,
            Status = model.Status,
            Remarks = model.Remarks,
            AssignedTo = model.AssignedTo
        };

        var (success, message, _) = await _followUpService.CreateFollowUpAsync(
            followUp,
            CurrentUserId,
            CurrentUserRole,
            CurrentUserName);

        if (!success)
        {
            ModelState.AddModelError(string.Empty, message);
            await PopulateDropDownsAsync(model);
            return View(model);
        }

        TempData["SuccessMessage"] = message;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var followUp = await _followUpService.GetFollowUpByIdAsync(id, CurrentUserId, CurrentUserRole);
        if (followUp == null)
        {
            return NotFound();
        }

        var model = new FollowUpCreateEditViewModel
        {
            FollowUpId = followUp.FollowUpId,
            CustomerId = followUp.CustomerId,
            LeadId = followUp.LeadId,
            OpportunityId = followUp.OpportunityId,
            FollowUpDate = followUp.FollowUpDate,
            Subject = followUp.Subject,
            FollowUpType = followUp.FollowUpType,
            Status = followUp.Status,
            Remarks = followUp.Remarks,
            AssignedTo = followUp.AssignedTo
        };

        await PopulateDropDownsAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, FollowUpCreateEditViewModel model)
    {
        if (id != model.FollowUpId)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            await PopulateDropDownsAsync(model);
            return View(model);
        }

        var followUp = new FollowUp
        {
            FollowUpId = model.FollowUpId,
            CustomerId = model.CustomerId,
            LeadId = model.LeadId,
            OpportunityId = model.OpportunityId,
            FollowUpDate = model.FollowUpDate,
            Subject = model.Subject.Trim(),
            FollowUpType = model.FollowUpType,
            Status = model.Status,
            Remarks = model.Remarks,
            AssignedTo = model.AssignedTo
        };

        var (success, message) = await _followUpService.UpdateFollowUpAsync(
            followUp,
            CurrentUserId,
            CurrentUserRole,
            CurrentUserName);

        if (!success)
        {
            ModelState.AddModelError(string.Empty, message);
            await PopulateDropDownsAsync(model);
            return View(model);
        }

        TempData["SuccessMessage"] = message;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(int id, string? remarks)
    {
        var (success, message) = await _followUpService.CompleteFollowUpAsync(
            id,
            remarks,
            CurrentUserId,
            CurrentUserRole,
            CurrentUserName);

        if (success)
            TempData["SuccessMessage"] = message;
        else
            TempData["ErrorMessage"] = message;

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reschedule(int id, DateTime newDate, string? remarks)
    {
        var (success, message) = await _followUpService.RescheduleFollowUpAsync(
            id,
            newDate,
            remarks,
            CurrentUserId,
            CurrentUserRole,
            CurrentUserName);

        if (success)
            TempData["SuccessMessage"] = message;
        else
            TempData["ErrorMessage"] = message;

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var (success, message) = await _followUpService.CancelFollowUpAsync(
            id,
            CurrentUserId,
            CurrentUserRole,
            CurrentUserName);

        if (success)
            TempData["SuccessMessage"] = message;
        else
            TempData["ErrorMessage"] = message;

        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateDropDownsAsync(FollowUpCreateEditViewModel model)
    {
        var allowedUserIds = await _scopeService.GetAllowedUserIdsAsync(CurrentUserId, CurrentUserRole);
        var customers = await _scopeService.FilterCustomers(_context.Customers.AsNoTracking().Where(c => c.Status == CustomerStatus.Active), allowedUserIds)
            .OrderBy(c => c.CustomerName).ToListAsync();
        var leads = await _scopeService.FilterLeads(_context.Leads.AsNoTracking(), allowedUserIds)
            .OrderBy(l => l.LeadName).ToListAsync();
        var opps = await _scopeService.FilterOpportunities(_context.Opportunities.AsNoTracking(), allowedUserIds)
            .OrderBy(o => o.OpportunityName).ToListAsync();
        var activeUsers = await _userService.GetActiveUsersAsync();

        model.AvailableCustomers = new SelectList(customers, "CustomerId", "CustomerName", model.CustomerId);
        model.AvailableLeads = new SelectList(leads, "LeadId", "LeadName", model.LeadId);
        model.AvailableOpportunities = new SelectList(opps, "OpportunityId", "OpportunityName", model.OpportunityId);
        model.AvailableAssignees = new SelectList(activeUsers, "Id", "FullName", model.AssignedTo);
    }
}

