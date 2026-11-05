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
public class OpportunitiesController : BaseController
{
    private readonly IOpportunityService _opportunityService;
    private readonly IUserService _userService;
    private readonly IRecordScopeService _scopeService;
    private readonly ApplicationDbContext _context;

    public OpportunitiesController(
        IOpportunityService opportunityService,
        IUserService userService,
        IRecordScopeService scopeService,
        ApplicationDbContext context)
    {
        _opportunityService = opportunityService;
        _userService = userService;
        _scopeService = scopeService;
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? searchTerm = null,
        OpportunityStage? stage = null,
        OpportunityStatus? status = null,
        string? assigneeId = null,
        string sortBy = "DateDesc",
        int page = 1)
    {
        var result = await _opportunityService.GetOpportunitiesAsync(
            CurrentUserId,
            CurrentUserRole,
            searchTerm,
            stage,
            status,
            assigneeId,
            sortBy,
            page,
            10);

        var activeUsers = await _userService.GetActiveUsersAsync();

        var model = new OpportunityListViewModel
        {
            Opportunities = result,
            SearchTerm = searchTerm,
            StageFilter = stage,
            StatusFilter = status,
            AssigneeFilter = assigneeId,
            SortBy = sortBy,
            AvailableAssignees = new SelectList(activeUsers, "Id", "FullName", assigneeId)
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var opportunity = await _opportunityService.GetOpportunityByIdAsync(id, CurrentUserId, CurrentUserRole, includeRelated: true);
        if (opportunity == null)
        {
            return NotFound();
        }

        var auditLogs = _context.AuditLogs
            .Where(a => a.EntityName == "Opportunity" && a.RecordId == id.ToString())
            .OrderByDescending(a => a.CreatedDate)
            .Take(10)
            .ToList();

        var model = new OpportunityDetailsViewModel
        {
            Opportunity = opportunity,
            RelatedFollowUps = opportunity.FollowUps.OrderByDescending(f => f.FollowUpDate).ToList(),
            RelatedActivities = opportunity.Activities.OrderByDescending(a => a.ActivityDate).ToList(),
            AuditLogs = auditLogs
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? customerId = null, int? leadId = null)
    {
        var allowedUserIds = await _scopeService.GetAllowedUserIdsAsync(CurrentUserId, CurrentUserRole);
        var customers = await _scopeService.FilterCustomers(_context.Customers.AsNoTracking().Where(c => c.Status == CustomerStatus.Active), allowedUserIds)
            .OrderBy(c => c.CustomerName).ToListAsync();
        var leads = await _scopeService.FilterLeads(_context.Leads.AsNoTracking(), allowedUserIds)
            .OrderBy(l => l.LeadName).ToListAsync();
        var activeUsers = await _userService.GetActiveUsersAsync();

        var model = new OpportunityCreateEditViewModel
        {
            CustomerId = customerId ?? (customers.FirstOrDefault()?.CustomerId ?? 0),
            LeadId = leadId,
            AssignedTo = CurrentUserId,
            ExpectedCloseDate = DateTime.Today.AddDays(30),
            Probability = 20,
            AvailableCustomers = new SelectList(customers, "CustomerId", "CustomerName", customerId),
            AvailableLeads = new SelectList(leads, "LeadId", "LeadName", leadId),
            AvailableAssignees = new SelectList(activeUsers, "Id", "FullName", CurrentUserId)
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(OpportunityCreateEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateDropDownsAsync(model);
            return View(model);
        }

        var opportunity = new Opportunity
        {
            OpportunityName = model.OpportunityName.Trim(),
            CustomerId = model.CustomerId,
            LeadId = model.LeadId,
            Amount = model.Amount,
            Stage = model.Stage,
            Probability = model.Probability,
            ExpectedCloseDate = model.ExpectedCloseDate,
            Status = model.Status,
            Source = model.Source,
            Notes = model.Notes,
            AssignedTo = model.AssignedTo
        };

        var (success, message, created) = await _opportunityService.CreateOpportunityAsync(
            opportunity,
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
        return RedirectToAction(nameof(Details), new { id = created!.OpportunityId });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var opportunity = await _opportunityService.GetOpportunityByIdAsync(id, CurrentUserId, CurrentUserRole);
        if (opportunity == null)
        {
            return NotFound();
        }

        var model = new OpportunityCreateEditViewModel
        {
            OpportunityId = opportunity.OpportunityId,
            OpportunityName = opportunity.OpportunityName,
            CustomerId = opportunity.CustomerId,
            LeadId = opportunity.LeadId,
            Amount = opportunity.Amount,
            Stage = opportunity.Stage,
            Probability = opportunity.Probability,
            ExpectedCloseDate = opportunity.ExpectedCloseDate,
            Status = opportunity.Status,
            Source = opportunity.Source,
            Notes = opportunity.Notes,
            AssignedTo = opportunity.AssignedTo
        };

        await PopulateDropDownsAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, OpportunityCreateEditViewModel model)
    {
        if (id != model.OpportunityId)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            await PopulateDropDownsAsync(model);
            return View(model);
        }

        var opportunity = new Opportunity
        {
            OpportunityId = model.OpportunityId,
            OpportunityName = model.OpportunityName.Trim(),
            CustomerId = model.CustomerId,
            LeadId = model.LeadId,
            Amount = model.Amount,
            Stage = model.Stage,
            Probability = model.Probability,
            ExpectedCloseDate = model.ExpectedCloseDate,
            Status = model.Status,
            Source = model.Source,
            Notes = model.Notes,
            AssignedTo = model.AssignedTo
        };

        var (success, message) = await _opportunityService.UpdateOpportunityAsync(
            opportunity,
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
        return RedirectToAction(nameof(Details), new { id = model.OpportunityId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStage(int id, OpportunityStage newStage)
    {
        var (success, message) = await _opportunityService.ChangeStageAsync(
            id,
            newStage,
            CurrentUserId,
            CurrentUserRole,
            CurrentUserName);

        if (success)
        {
            TempData["SuccessMessage"] = message;
        }
        else
        {
            TempData["ErrorMessage"] = message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task PopulateDropDownsAsync(OpportunityCreateEditViewModel model)
    {
        var allowedUserIds = await _scopeService.GetAllowedUserIdsAsync(CurrentUserId, CurrentUserRole);
        var customers = await _scopeService.FilterCustomers(_context.Customers.AsNoTracking().Where(c => c.Status == CustomerStatus.Active), allowedUserIds)
            .OrderBy(c => c.CustomerName).ToListAsync();
        var leads = await _scopeService.FilterLeads(_context.Leads.AsNoTracking(), allowedUserIds)
            .OrderBy(l => l.LeadName).ToListAsync();
        var activeUsers = await _userService.GetActiveUsersAsync();

        model.AvailableCustomers = new SelectList(customers, "CustomerId", "CustomerName", model.CustomerId);
        model.AvailableLeads = new SelectList(leads, "LeadId", "LeadName", model.LeadId);
        model.AvailableAssignees = new SelectList(activeUsers, "Id", "FullName", model.AssignedTo);
    }
}

