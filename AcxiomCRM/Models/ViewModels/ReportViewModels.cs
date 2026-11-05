using AcxiomCRM.Models.Entities;

namespace AcxiomCRM.Models.ViewModels;

public class BaseReportFilter
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? SearchTerm { get; set; }
    public string? OwnerId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class PipelineReportViewModel
{
    public decimal TotalOpenValue { get; set; }
    public decimal TotalWeightedValue { get; set; }
    public int TotalOpenCount { get; set; }
    public List<StageSummaryItem> StageSummaries { get; set; } = new();
    public List<OwnerSummaryItem> OwnerSummaries { get; set; } = new();
    public PagedResult<Opportunity> Opportunities { get; set; } = new();
}

public class StageSummaryItem
{
    public string Stage { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal WeightedAmount { get; set; }
}

public class OwnerSummaryItem
{
    public string OwnerName { get; set; } = string.Empty;
    public int OpenCount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal WeightedAmount { get; set; }
}

public class ConversionReportViewModel
{
    public int TotalLeads { get; set; }
    public int ConvertedLeads { get; set; }
    public int UnconvertedLeads { get; set; }
    public double ConversionRate { get; set; }
    public int WonOpportunities { get; set; }
    public int LostOpportunities { get; set; }
    public decimal WonRevenue { get; set; }
    public decimal LostRevenue { get; set; }
}

public class UserActivityReportItem
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int TotalActivities { get; set; }
    public int CallsCount { get; set; }
    public int MeetingsCount { get; set; }
    public int EmailsCount { get; set; }
    public int TasksCount { get; set; }
    public int AuditActionsCount { get; set; }
}

public class UserActivityReportViewModel
{
    public List<UserActivityReportItem> UserActivities { get; set; } = new();
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

