using AcxiomCRM.Models.Entities;

namespace AcxiomCRM.Models.ViewModels;

public class DashboardViewModel
{
    public string UserRole { get; set; } = string.Empty;
    public string UserFullName { get; set; } = string.Empty;

    // Filters
    public string DateFilter { get; set; } = "ThisMonth"; // Today, ThisWeek, ThisMonth, Custom, All
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    // KPI Cards
    public int TotalCustomers { get; set; }
    public int TotalLeads { get; set; }
    public int OpenLeads { get; set; }
    public int TotalOpportunities { get; set; }
    public int OpenOpportunities { get; set; }
    public int WonOpportunities { get; set; }
    public int LostOpportunities { get; set; }
    public decimal TotalPipelineValue { get; set; }
    public decimal WeightedPipelineValue { get; set; }

    // Follow-ups & Activity
    public int UpcomingFollowUpsCount { get; set; }
    public int OverdueFollowUpsCount { get; set; }
    public List<FollowUp> UpcomingFollowUps { get; set; } = new();
    public List<FollowUp> OverdueFollowUps { get; set; } = new();

    // Chart Data (Pre-serialized or strongly typed for Chart.js)
    public List<string> LeadStatusLabels { get; set; } = new();
    public List<int> LeadStatusCounts { get; set; } = new();

    public List<string> OpportunityStageLabels { get; set; } = new();
    public List<int> OpportunityStageCounts { get; set; } = new();

    public List<string> MonthlySalesLabels { get; set; } = new();
    public List<decimal> MonthlyWonAmounts { get; set; } = new();
    public List<decimal> MonthlyPipelineAmounts { get; set; } = new();

    // Recent items
    public List<Lead> RecentLeads { get; set; } = new();
    public List<Opportunity> RecentOpportunities { get; set; } = new();
}

