using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.Authorization;
using AcxiomCRM.Services;

namespace AcxiomCRM.Controllers;

[Authorize(Policy = AuthorizationPolicies.RequireAdmin)]
public class AuditController : BaseController
{
    private readonly IAuditService _auditService;

    public AuditController(IAuditService auditService)
    {
        _auditService = auditService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? searchTerm = null,
        string? module = null,
        string? actionName = null,
        string? result = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int page = 1)
    {
        var (items, totalCount) = await _auditService.GetAuditLogsAsync(
            searchTerm,
            module,
            actionName,
            result,
            startDate,
            endDate,
            page,
            20);

        ViewBag.SearchTerm = searchTerm;
        ViewBag.Module = module;
        ViewBag.ActionName = actionName;
        ViewBag.Result = result;
        ViewBag.StartDate = startDate;
        ViewBag.EndDate = endDate;
        ViewBag.CurrentPage = page;
        ViewBag.PageSize = 20;
        ViewBag.TotalCount = totalCount;
        ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / 20);

        return View(items);
    }
}

