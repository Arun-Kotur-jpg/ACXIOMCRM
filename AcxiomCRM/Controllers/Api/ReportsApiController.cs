using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.Authorization;
using AcxiomCRM.Models.DTOs;
using AcxiomCRM.Models.ViewModels;
using AcxiomCRM.Services;

namespace AcxiomCRM.Controllers.Api;

[ApiController]
[Route("api/reports")]
[Authorize]
public class ReportsApiController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsApiController(IReportService reportService)
    {
        _reportService = reportService;
    }

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
    private string CurrentUserRole
    {
        get
        {
            if (User.IsInRole(RoleConstants.Admin)) return RoleConstants.Admin;
            if (User.IsInRole(RoleConstants.Manager)) return RoleConstants.Manager;
            return RoleConstants.SalesExecutive;
        }
    }

    [HttpGet("pipeline")]
    public async Task<IActionResult> GetPipelineReport()
    {
        var report = await _reportService.GetPipelineReportAsync(CurrentUserId, CurrentUserRole, new BaseReportFilter());

        var stageValues = report.StageSummaries.ToDictionary(s => s.Stage, s => s.TotalAmount);

        return Ok(new PipelineReportDto
        {
            TotalOpenValue = report.TotalOpenValue,
            TotalWeightedValue = report.TotalWeightedValue,
            TotalOpenCount = report.TotalOpenCount,
            StageValues = stageValues
        });
    }
}

