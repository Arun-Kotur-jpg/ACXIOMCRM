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
public class ActivitiesController : BaseController
{
    private readonly IActivityService _activityService;
    private readonly IUserService _userService;
    private readonly IRecordScopeService _scopeService;
    private readonly ApplicationDbContext _context;

    public ActivitiesController(
        IActivityService activityService,
        IUserService userService,
        IRecordScopeService scopeService,
        ApplicationDbContext context)
    {
        _activityService = activityService;
        _userService = userService;
        _scopeService = scopeService;
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? searchTerm = null,
        ActivityType? type = null,
        ActivityStatus? status = null,
        string? assigneeId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int page = 1)
    {
        var result = await _activityService.GetActivitiesAsync(
            CurrentUserId,
            CurrentUserRole,
            searchTerm,
            type,
            status,
            assigneeId,
            startDate,
            endDate,
            page,
            10);

        var activeUsers = await _userService.GetActiveUsersAsync();

        var model = new ActivityListViewModel
        {
            Activities = result,
            SearchTerm = searchTerm,
            TypeFilter = type,
            StatusFilter = status,
            AssigneeFilter = assigneeId,
            StartDate = startDate,
            EndDate = endDate,
            AvailableAssignees = new SelectList(activeUsers, "Id", "FullName", assigneeId)
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? customerId = null, int? leadId = null, int? opportunityId = null)
    {
        var model = new ActivityCreateEditViewModel
        {
            CustomerId = customerId,
            LeadId = leadId,
            OpportunityId = opportunityId,
            ActivityDate = DateTime.Now,
            AssignedTo = CurrentUserId
        };

        await PopulateDropDownsAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ActivityCreateEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateDropDownsAsync(model);
            return View(model);
        }

        var activity = new Activity
        {
            ActivityType = model.ActivityType,
            Subject = model.Subject.Trim(),
            Description = model.Description,
            ActivityDate = model.ActivityDate,
            CustomerId = model.CustomerId,
            LeadId = model.LeadId,
            OpportunityId = model.OpportunityId,
            Status = model.Status,
            AssignedTo = model.AssignedTo
        };

        var (success, message, _) = await _activityService.CreateActivityAsync(
            activity,
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
        var activity = await _activityService.GetActivityByIdAsync(id, CurrentUserId, CurrentUserRole);
        if (activity == null)
        {
            return NotFound();
        }

        var model = new ActivityCreateEditViewModel
        {
            ActivityId = activity.ActivityId,
            ActivityType = activity.ActivityType,
            Subject = activity.Subject,
            Description = activity.Description,
            ActivityDate = activity.ActivityDate,
            CustomerId = activity.CustomerId,
            LeadId = activity.LeadId,
            OpportunityId = activity.OpportunityId,
            Status = activity.Status,
            AssignedTo = activity.AssignedTo
        };

        await PopulateDropDownsAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ActivityCreateEditViewModel model)
    {
        if (id != model.ActivityId)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            await PopulateDropDownsAsync(model);
            return View(model);
        }

        var activity = new Activity
        {
            ActivityId = model.ActivityId,
            ActivityType = model.ActivityType,
            Subject = model.Subject.Trim(),
            Description = model.Description,
            ActivityDate = model.ActivityDate,
            CustomerId = model.CustomerId,
            LeadId = model.LeadId,
            OpportunityId = model.OpportunityId,
            Status = model.Status,
            AssignedTo = model.AssignedTo
        };

        var (success, message) = await _activityService.UpdateActivityAsync(
            activity,
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
    public async Task<IActionResult> Complete(int id)
    {
        var (success, message) = await _activityService.CompleteActivityAsync(
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

    private async Task PopulateDropDownsAsync(ActivityCreateEditViewModel model)
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

