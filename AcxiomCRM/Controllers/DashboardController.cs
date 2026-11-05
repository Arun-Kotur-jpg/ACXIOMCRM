using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.Services;

namespace AcxiomCRM.Controllers;

[Authorize]
public class DashboardController : BaseController
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string dateFilter = "ThisMonth", DateTime? startDate = null, DateTime? endDate = null)
    {
        var model = await _dashboardService.GetDashboardDataAsync(
            CurrentUserId,
            CurrentUserRole,
            CurrentUserName,
            dateFilter,
            startDate,
            endDate);

        return View(model);
    }
}

