using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using AcxiomCRM.Authorization;
using AcxiomCRM.Models.Enums;
using AcxiomCRM.Models.ViewModels;
using AcxiomCRM.Services;

namespace AcxiomCRM.Controllers;

[Authorize]
public class ReportsController : BaseController
{
    private readonly IReportService _reportService;
    private readonly IUserService _userService;

    public ReportsController(
        IReportService reportService,
        IUserService userService)
    {
        _reportService = reportService;
        _userService = userService;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> Customers(BaseReportFilter filter)
    {
        var result = await _reportService.GetCustomerReportAsync(CurrentUserId, CurrentUserRole, filter);
        var activeUsers = await _userService.GetActiveUsersAsync();
        ViewBag.AvailableOwners = new SelectList(activeUsers, "Id", "FullName", filter.OwnerId);
        ViewBag.Filter = filter;
        return View(result);
    }

    [HttpGet]
    public async Task<IActionResult> ExportCustomersCsv(BaseReportFilter filter)
    {
        var bytes = await _reportService.ExportCustomersCsvAsync(CurrentUserId, CurrentUserRole, filter);
        return File(bytes, "text/csv", $"Customers_Report_{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    [HttpGet]
    public async Task<IActionResult> Leads(BaseReportFilter filter, LeadStatus? status = null)
    {
        var result = await _reportService.GetLeadReportAsync(CurrentUserId, CurrentUserRole, filter, status);
        var activeUsers = await _userService.GetActiveUsersAsync();
        ViewBag.AvailableOwners = new SelectList(activeUsers, "Id", "FullName", filter.OwnerId);
        ViewBag.Filter = filter;
        ViewBag.Status = status;
        return View(result);
    }

    [HttpGet]
    public async Task<IActionResult> ExportLeadsCsv(BaseReportFilter filter)
    {
        var bytes = await _reportService.ExportLeadsCsvAsync(CurrentUserId, CurrentUserRole, filter);
        return File(bytes, "text/csv", $"Leads_Report_{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    [HttpGet]
    public async Task<IActionResult> Opportunities(BaseReportFilter filter, OpportunityStage? stage = null)
    {
        var result = await _reportService.GetOpportunityReportAsync(CurrentUserId, CurrentUserRole, filter, stage);
        var activeUsers = await _userService.GetActiveUsersAsync();
        ViewBag.AvailableOwners = new SelectList(activeUsers, "Id", "FullName", filter.OwnerId);
        ViewBag.Filter = filter;
        ViewBag.Stage = stage;
        return View(result);
    }

    [HttpGet]
    public async Task<IActionResult> ExportOpportunitiesCsv(BaseReportFilter filter)
    {
        var bytes = await _reportService.ExportOpportunitiesCsvAsync(CurrentUserId, CurrentUserRole, filter);
        return File(bytes, "text/csv", $"Opportunities_Report_{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    [HttpGet]
    public async Task<IActionResult> FollowUps(BaseReportFilter filter, FollowUpStatus? status = null)
    {
        var result = await _reportService.GetFollowUpReportAsync(CurrentUserId, CurrentUserRole, filter, status);
        var activeUsers = await _userService.GetActiveUsersAsync();
        ViewBag.AvailableOwners = new SelectList(activeUsers, "Id", "FullName", filter.OwnerId);
        ViewBag.Filter = filter;
        ViewBag.Status = status;
        return View(result);
    }

    [HttpGet]
    public async Task<IActionResult> ExportFollowUpsCsv(BaseReportFilter filter)
    {
        var bytes = await _reportService.ExportFollowUpsCsvAsync(CurrentUserId, CurrentUserRole, filter);
        return File(bytes, "text/csv", $"FollowUps_Report_{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    [HttpGet]
    public async Task<IActionResult> Pipeline(BaseReportFilter filter)
    {
        var result = await _reportService.GetPipelineReportAsync(CurrentUserId, CurrentUserRole, filter);
        var activeUsers = await _userService.GetActiveUsersAsync();
        ViewBag.AvailableOwners = new SelectList(activeUsers, "Id", "FullName", filter.OwnerId);
        ViewBag.Filter = filter;
        return View(result);
    }

    [HttpGet]
    public async Task<IActionResult> ExportPipelineCsv(BaseReportFilter filter)
    {
        var bytes = await _reportService.ExportPipelineCsvAsync(CurrentUserId, CurrentUserRole, filter);
        return File(bytes, "text/csv", $"Pipeline_Report_{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    [HttpGet]
    public async Task<IActionResult> Conversion(DateTime? startDate = null, DateTime? endDate = null)
    {
        var result = await _reportService.GetConversionReportAsync(CurrentUserId, CurrentUserRole, startDate, endDate);
        ViewBag.StartDate = startDate;
        ViewBag.EndDate = endDate;
        return View(result);
    }

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.RequireManagerOrAdmin)]
    public async Task<IActionResult> UserActivity(DateTime? startDate = null, DateTime? endDate = null)
    {
        var result = await _reportService.GetUserActivityReportAsync(CurrentUserId, CurrentUserRole, startDate, endDate);
        return View(result);
    }
}

