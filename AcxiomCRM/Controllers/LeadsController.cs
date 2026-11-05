using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using AcxiomCRM.Data;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Models.Enums;
using AcxiomCRM.Models.ViewModels;
using AcxiomCRM.Services;

namespace AcxiomCRM.Controllers;

[Authorize]
public class LeadsController : BaseController
{
    private readonly ILeadService _leadService;
    private readonly IUserService _userService;
    private readonly ApplicationDbContext _context;

    public LeadsController(
        ILeadService leadService,
        IUserService userService,
        ApplicationDbContext context)
    {
        _leadService = leadService;
        _userService = userService;
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? searchTerm = null,
        LeadStatus? status = null,
        LeadPriority? priority = null,
        LeadSource? source = null,
        string? assigneeId = null,
        string sortBy = "DateDesc",
        int page = 1)
    {
        var result = await _leadService.GetLeadsAsync(
            CurrentUserId,
            CurrentUserRole,
            searchTerm,
            status,
            priority,
            source,
            assigneeId,
            sortBy,
            page,
            10);

        var activeUsers = await _userService.GetActiveUsersAsync();

        var model = new LeadListViewModel
        {
            Leads = result,
            SearchTerm = searchTerm,
            StatusFilter = status,
            PriorityFilter = priority,
            SourceFilter = source,
            AssigneeFilter = assigneeId,
            SortBy = sortBy,
            AvailableAssignees = new SelectList(activeUsers, "Id", "FullName", assigneeId)
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var lead = await _leadService.GetLeadByIdAsync(id, CurrentUserId, CurrentUserRole, includeRelated: true);
        if (lead == null)
        {
            return NotFound();
        }

        var auditLogs = _context.AuditLogs
            .Where(a => a.EntityName == "Lead" && a.RecordId == id.ToString())
            .OrderByDescending(a => a.CreatedDate)
            .Take(10)
            .ToList();

        var model = new LeadDetailsViewModel
        {
            Lead = lead,
            RelatedOpportunities = lead.Opportunities.OrderByDescending(o => o.CreatedDate).ToList(),
            RelatedFollowUps = lead.FollowUps.OrderByDescending(f => f.FollowUpDate).ToList(),
            RelatedActivities = lead.Activities.OrderByDescending(a => a.ActivityDate).ToList(),
            AuditLogs = auditLogs
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var activeUsers = await _userService.GetActiveUsersAsync();
        var model = new LeadCreateEditViewModel
        {
            AssignedTo = CurrentUserId,
            AvailableAssignees = new SelectList(activeUsers, "Id", "FullName", CurrentUserId)
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(LeadCreateEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var activeUsers = await _userService.GetActiveUsersAsync();
            model.AvailableAssignees = new SelectList(activeUsers, "Id", "FullName", model.AssignedTo);
            return View(model);
        }

        var lead = new Lead
        {
            LeadName = model.LeadName.Trim(),
            Email = model.Email?.Trim(),
            Phone = model.Phone.Trim(),
            CompanyName = model.CompanyName,
            Source = model.Source,
            Status = model.Status,
            Priority = model.Priority,
            ExpectedValue = model.ExpectedValue,
            Notes = model.Notes,
            AssignedTo = model.AssignedTo
        };

        var (success, message, created) = await _leadService.CreateLeadAsync(
            lead,
            CurrentUserId,
            CurrentUserRole,
            CurrentUserName);

        if (!success)
        {
            ModelState.AddModelError(string.Empty, message);
            var activeUsers = await _userService.GetActiveUsersAsync();
            model.AvailableAssignees = new SelectList(activeUsers, "Id", "FullName", model.AssignedTo);
            return View(model);
        }

        TempData["SuccessMessage"] = message;
        return RedirectToAction(nameof(Details), new { id = created!.LeadId });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var lead = await _leadService.GetLeadByIdAsync(id, CurrentUserId, CurrentUserRole);
        if (lead == null)
        {
            return NotFound();
        }

        var activeUsers = await _userService.GetActiveUsersAsync();
        var model = new LeadCreateEditViewModel
        {
            LeadId = lead.LeadId,
            LeadCode = lead.LeadCode,
            LeadName = lead.LeadName,
            Email = lead.Email,
            Phone = lead.Phone,
            CompanyName = lead.CompanyName,
            Source = lead.Source,
            Status = lead.Status,
            Priority = lead.Priority,
            ExpectedValue = lead.ExpectedValue,
            Notes = lead.Notes,
            AssignedTo = lead.AssignedTo,
            AvailableAssignees = new SelectList(activeUsers, "Id", "FullName", lead.AssignedTo)
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, LeadCreateEditViewModel model)
    {
        if (id != model.LeadId)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            var activeUsers = await _userService.GetActiveUsersAsync();
            model.AvailableAssignees = new SelectList(activeUsers, "Id", "FullName", model.AssignedTo);
            return View(model);
        }

        var lead = new Lead
        {
            LeadId = model.LeadId,
            LeadName = model.LeadName.Trim(),
            Email = model.Email?.Trim(),
            Phone = model.Phone.Trim(),
            CompanyName = model.CompanyName,
            Source = model.Source,
            Status = model.Status,
            Priority = model.Priority,
            ExpectedValue = model.ExpectedValue,
            Notes = model.Notes,
            AssignedTo = model.AssignedTo
        };

        var (success, message) = await _leadService.UpdateLeadAsync(
            lead,
            CurrentUserId,
            CurrentUserRole,
            CurrentUserName);

        if (!success)
        {
            ModelState.AddModelError(string.Empty, message);
            var activeUsers = await _userService.GetActiveUsersAsync();
            model.AvailableAssignees = new SelectList(activeUsers, "Id", "FullName", model.AssignedTo);
            return View(model);
        }

        TempData["SuccessMessage"] = message;
        return RedirectToAction(nameof(Details), new { id = model.LeadId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStatus(int id, LeadStatus newStatus)
    {
        var (success, message) = await _leadService.ChangeStatusAsync(
            id,
            newStatus,
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

    [HttpGet]
    public async Task<IActionResult> Convert(int id)
    {
        var lead = await _leadService.GetLeadByIdAsync(id, CurrentUserId, CurrentUserRole);
        if (lead == null)
        {
            return NotFound();
        }

        if (lead.Status == LeadStatus.Converted)
        {
            TempData["ErrorMessage"] = "This lead has already been converted.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var activeUsers = await _userService.GetActiveUsersAsync();
        var model = new LeadConvertViewModel
        {
            LeadId = lead.LeadId,
            LeadCode = lead.LeadCode,
            CustomerName = lead.LeadName,
            Email = lead.Email ?? string.Empty,
            Phone = lead.Phone,
            CompanyName = lead.CompanyName,
            CreateOpportunity = true,
            OpportunityName = $"{lead.LeadName} - Deal",
            OpportunityAmount = lead.ExpectedValue ?? 100000m,
            OpportunityProbability = 25,
            ExpectedCloseDate = DateTime.Today.AddDays(30),
            AssignedTo = lead.AssignedTo,
            AvailableAssignees = new SelectList(activeUsers, "Id", "FullName", lead.AssignedTo)
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Convert(LeadConvertViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var activeUsers = await _userService.GetActiveUsersAsync();
            model.AvailableAssignees = new SelectList(activeUsers, "Id", "FullName", model.AssignedTo);
            return View(model);
        }

        var (success, message, customerId, _) = await _leadService.ConvertLeadAsync(
            model,
            CurrentUserId,
            CurrentUserRole,
            CurrentUserName);

        if (!success)
        {
            ModelState.AddModelError(string.Empty, message);
            var activeUsers = await _userService.GetActiveUsersAsync();
            model.AvailableAssignees = new SelectList(activeUsers, "Id", "FullName", model.AssignedTo);
            return View(model);
        }

        TempData["SuccessMessage"] = message;
        return RedirectToAction("Details", "Customers", new { id = customerId });
    }
}

