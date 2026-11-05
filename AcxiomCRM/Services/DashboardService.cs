using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.Models.Enums;
using AcxiomCRM.Models.ViewModels;

namespace AcxiomCRM.Services;

public interface IDashboardService
{
    Task<DashboardViewModel> GetDashboardDataAsync(
        string userId,
        string role,
        string userFullName,
        string dateFilter = "ThisMonth",
        DateTime? customStart = null,
        DateTime? customEnd = null);
}

public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _context;
    private readonly IRecordScopeService _scopeService;

    public DashboardService(ApplicationDbContext context, IRecordScopeService scopeService)
    {
        _context = context;
        _scopeService = scopeService;
    }

    public async Task<DashboardViewModel> GetDashboardDataAsync(
        string userId,
        string role,
        string userFullName,
        string dateFilter = "ThisMonth",
        DateTime? customStart = null,
        DateTime? customEnd = null)
    {
        var allowedUserIds = await _scopeService.GetAllowedUserIdsAsync(userId, role);

        // Date filter range calculation
        DateTime? startDate = null;
        DateTime? endDate = null;
        var now = DateTime.UtcNow;

        switch (dateFilter)
        {
            case "Today":
                startDate = DateTime.UtcNow.Date;
                endDate = startDate.Value.AddDays(1).AddTicks(-1);
                break;
            case "ThisWeek":
                var diff = (7 + (now.DayOfWeek - DayOfWeek.Monday)) % 7;
                startDate = now.Date.AddDays(-1 * diff);
                endDate = startDate.Value.AddDays(7).AddTicks(-1);
                break;
            case "ThisMonth":
                startDate = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                endDate = startDate.Value.AddMonths(1).AddTicks(-1);
                break;
            case "Custom":
                startDate = customStart?.ToUniversalTime();
                endDate = customEnd.HasValue ? customEnd.Value.Date.AddDays(1).AddTicks(-1).ToUniversalTime() : null;
                break;
            case "All":
            default:
                break;
        }

        // Base scoped queries
        var customersQuery = _scopeService.FilterCustomers(_context.Customers.AsNoTracking(), allowedUserIds);
        var leadsQuery = _scopeService.FilterLeads(_context.Leads.AsNoTracking(), allowedUserIds);
        var oppsQuery = _scopeService.FilterOpportunities(_context.Opportunities.AsNoTracking(), allowedUserIds);
        var followUpsQuery = _scopeService.FilterFollowUps(_context.FollowUps.AsNoTracking(), allowedUserIds);

        // Apply date filtering where appropriate
        if (startDate.HasValue)
        {
            customersQuery = customersQuery.Where(c => c.CreatedDate >= startDate.Value);
            leadsQuery = leadsQuery.Where(l => l.CreatedDate >= startDate.Value);
            oppsQuery = oppsQuery.Where(o => o.CreatedDate >= startDate.Value);
        }
        if (endDate.HasValue)
        {
            customersQuery = customersQuery.Where(c => c.CreatedDate <= endDate.Value);
            leadsQuery = leadsQuery.Where(l => l.CreatedDate <= endDate.Value);
            oppsQuery = oppsQuery.Where(o => o.CreatedDate <= endDate.Value);
        }

        // KPI Counts
        var totalCustomers = await customersQuery.CountAsync();
        var totalLeads = await leadsQuery.CountAsync();
        var openLeads = await leadsQuery.CountAsync(l => l.Status == LeadStatus.New || l.Status == LeadStatus.Contacted || l.Status == LeadStatus.Qualified);

        var totalOpps = await oppsQuery.CountAsync();
        var openOpps = await oppsQuery.CountAsync(o => o.Status == OpportunityStatus.Open);
        var wonOpps = await oppsQuery.CountAsync(o => o.Status == OpportunityStatus.Won);
        var lostOpps = await oppsQuery.CountAsync(o => o.Status == OpportunityStatus.Lost);

        // Pipeline calculation - only OPEN opportunities
        var openOpportunities = await oppsQuery
            .Where(o => o.Status == OpportunityStatus.Open)
            .Select(o => new { o.Amount, o.Probability })
            .ToListAsync();

        var totalPipelineValue = openOpportunities.Sum(o => o.Amount);
        var weightedPipelineValue = openOpportunities.Sum(o => o.Amount * o.Probability / 100m);

        // Follow-ups (Active / Current)
        var allScopedFollowUps = _scopeService.FilterFollowUps(
            _context.FollowUps.AsNoTracking().Include(f => f.Customer).Include(f => f.Lead),
            allowedUserIds);

        var upcomingFollowUps = await allScopedFollowUps
            .Where(f => f.Status == FollowUpStatus.Planned && f.FollowUpDate >= now)
            .OrderBy(f => f.FollowUpDate)
            .Take(5)
            .ToListAsync();

        var overdueFollowUps = await allScopedFollowUps
            .Where(f => f.Status == FollowUpStatus.Planned && f.FollowUpDate < now)
            .OrderBy(f => f.FollowUpDate)
            .Take(5)
            .ToListAsync();

        var upcomingCount = await allScopedFollowUps.CountAsync(f => f.Status == FollowUpStatus.Planned && f.FollowUpDate >= now);
        var overdueCount = await allScopedFollowUps.CountAsync(f => f.Status == FollowUpStatus.Planned && f.FollowUpDate < now);

        // Recent items
        var recentLeads = await _scopeService.FilterLeads(_context.Leads.AsNoTracking().Include(l => l.AssignedUser), allowedUserIds)
            .OrderByDescending(l => l.CreatedDate)
            .Take(5)
            .ToListAsync();

        var recentOpps = await _scopeService.FilterOpportunities(_context.Opportunities.AsNoTracking().Include(o => o.Customer), allowedUserIds)
            .OrderByDescending(o => o.CreatedDate)
            .Take(5)
            .ToListAsync();

        // 1. Lead Status Chart
        var leadStatuses = new[]
        {
            LeadStatus.New,
            LeadStatus.Contacted,
            LeadStatus.Qualified,
            LeadStatus.Unqualified,
            LeadStatus.Converted,
            LeadStatus.Lost
        };

        var leadCountsGrouped = await leadsQuery
            .GroupBy(l => l.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        var leadStatusLabels = new List<string>();
        var leadStatusCounts = new List<int>();

        foreach (var status in leadStatuses)
        {
            leadStatusLabels.Add(status.ToString());
            var matched = leadCountsGrouped.FirstOrDefault(g => g.Status == status);
            leadStatusCounts.Add(matched?.Count ?? 0);
        }

        // 2. Opportunity Pipeline Chart
        var oppStages = new[]
        {
            OpportunityStage.Qualification,
            OpportunityStage.Proposal,
            OpportunityStage.Negotiation,
            OpportunityStage.Won,
            OpportunityStage.Lost
        };

        var oppStagesGrouped = await oppsQuery
            .GroupBy(o => o.Stage)
            .Select(g => new { Stage = g.Key, Count = g.Count() })
            .ToListAsync();

        var oppStageLabels = new List<string>();
        var oppStageCounts = new List<int>();

        foreach (var stage in oppStages)
        {
            oppStageLabels.Add(stage.ToString());
            var matched = oppStagesGrouped.FirstOrDefault(g => g.Stage == stage);
            oppStageCounts.Add(matched?.Count ?? 0);
        }

        // 3. Monthly Sales Chart (Last 6 calendar months)
        var monthlyLabels = new List<string>();
        var monthlyWon = new List<decimal>();
        var monthlyPipeline = new List<decimal>();

        // Query all opportunities in the last 6 months for trend
        var sixMonthsAgo = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-5);
        var trendOpps = await _scopeService.FilterOpportunities(_context.Opportunities.AsNoTracking(), allowedUserIds)
            .Where(o => o.CreatedDate >= sixMonthsAgo)
            .Select(o => new { o.CreatedDate, o.Amount, o.Status, o.Stage })
            .ToListAsync();

        for (int i = 5; i >= 0; i--)
        {
            var targetMonth = now.AddMonths(-i);
            var mYear = targetMonth.Year;
            var mMonth = targetMonth.Month;
            var label = targetMonth.ToString("MMM yyyy");
            monthlyLabels.Add(label);

            var inMonth = trendOpps.Where(o => o.CreatedDate.Year == mYear && o.CreatedDate.Month == mMonth).ToList();
            var wonSum = inMonth.Where(o => o.Stage == OpportunityStage.Won || o.Status == OpportunityStatus.Won).Sum(o => o.Amount);
            var pipeSum = inMonth.Where(o => o.Status == OpportunityStatus.Open).Sum(o => o.Amount);

            monthlyWon.Add(wonSum);
            monthlyPipeline.Add(pipeSum);
        }

        return new DashboardViewModel
        {
            UserRole = role,
            UserFullName = userFullName,
            DateFilter = dateFilter,
            StartDate = startDate,
            EndDate = endDate,
            TotalCustomers = totalCustomers,
            TotalLeads = totalLeads,
            OpenLeads = openLeads,
            TotalOpportunities = totalOpps,
            OpenOpportunities = openOpps,
            WonOpportunities = wonOpps,
            LostOpportunities = lostOpps,
            TotalPipelineValue = totalPipelineValue,
            WeightedPipelineValue = weightedPipelineValue,
            UpcomingFollowUpsCount = upcomingCount,
            OverdueFollowUpsCount = overdueCount,
            UpcomingFollowUps = upcomingFollowUps,
            OverdueFollowUps = overdueFollowUps,
            LeadStatusLabels = leadStatusLabels,
            LeadStatusCounts = leadStatusCounts,
            OpportunityStageLabels = oppStageLabels,
            OpportunityStageCounts = oppStageCounts,
            MonthlySalesLabels = monthlyLabels,
            MonthlyWonAmounts = monthlyWon,
            MonthlyPipelineAmounts = monthlyPipeline,
            RecentLeads = recentLeads,
            RecentOpportunities = recentOpps
        };
    }
}

