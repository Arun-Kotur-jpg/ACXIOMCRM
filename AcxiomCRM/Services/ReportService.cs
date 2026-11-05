using System.Text;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Authorization;
using AcxiomCRM.Data;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Models.Enums;
using AcxiomCRM.Models.ViewModels;

namespace AcxiomCRM.Services;

public interface IReportService
{
    Task<PagedResult<Customer>> GetCustomerReportAsync(string userId, string role, BaseReportFilter filter);
    Task<PagedResult<Lead>> GetLeadReportAsync(string userId, string role, BaseReportFilter filter, LeadStatus? status = null);
    Task<PagedResult<FollowUp>> GetFollowUpReportAsync(string userId, string role, BaseReportFilter filter, FollowUpStatus? status = null);
    Task<PagedResult<Opportunity>> GetOpportunityReportAsync(string userId, string role, BaseReportFilter filter, OpportunityStage? stage = null);
    Task<PipelineReportViewModel> GetPipelineReportAsync(string userId, string role, BaseReportFilter filter);
    Task<ConversionReportViewModel> GetConversionReportAsync(string userId, string role, DateTime? startDate = null, DateTime? endDate = null);
    Task<UserActivityReportViewModel> GetUserActivityReportAsync(string userId, string role, DateTime? startDate = null, DateTime? endDate = null);

    // CSV Exports
    Task<byte[]> ExportCustomersCsvAsync(string userId, string role, BaseReportFilter filter);
    Task<byte[]> ExportLeadsCsvAsync(string userId, string role, BaseReportFilter filter);
    Task<byte[]> ExportOpportunitiesCsvAsync(string userId, string role, BaseReportFilter filter);
    Task<byte[]> ExportFollowUpsCsvAsync(string userId, string role, BaseReportFilter filter);
    Task<byte[]> ExportPipelineCsvAsync(string userId, string role, BaseReportFilter filter);
}

public class ReportService : IReportService
{
    private readonly ApplicationDbContext _context;
    private readonly IRecordScopeService _scopeService;

    public ReportService(ApplicationDbContext context, IRecordScopeService scopeService)
    {
        _context = context;
        _scopeService = scopeService;
    }

    public async Task<PagedResult<Customer>> GetCustomerReportAsync(string userId, string role, BaseReportFilter filter)
    {
        var allowedUserIds = await _scopeService.GetAllowedUserIdsAsync(userId, role);
        var query = _scopeService.FilterCustomers(_context.Customers.AsNoTracking().Include(c => c.Owner), allowedUserIds);

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.Trim();
            query = query.Where(c => c.CustomerName.Contains(term) || c.Email.Contains(term) || c.Phone.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(filter.OwnerId))
        {
            query = query.Where(c => c.OwnerId == filter.OwnerId);
        }

        if (filter.StartDate.HasValue)
        {
            query = query.Where(c => c.CreatedDate >= filter.StartDate.Value.ToUniversalTime());
        }

        if (filter.EndDate.HasValue)
        {
            var endOfDay = filter.EndDate.Value.Date.AddDays(1).AddTicks(-1).ToUniversalTime();
            query = query.Where(c => c.CreatedDate <= endOfDay);
        }

        query = query.OrderByDescending(c => c.CreatedDate);

        var total = await query.CountAsync();
        var items = await query.Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize).ToListAsync();

        return new PagedResult<Customer>
        {
            Items = items,
            TotalCount = total,
            PageNumber = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<PagedResult<Lead>> GetLeadReportAsync(string userId, string role, BaseReportFilter filter, LeadStatus? status = null)
    {
        var allowedUserIds = await _scopeService.GetAllowedUserIdsAsync(userId, role);
        var query = _scopeService.FilterLeads(_context.Leads.AsNoTracking().Include(l => l.AssignedUser).Include(l => l.Customer), allowedUserIds);

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.Trim();
            query = query.Where(l => l.LeadName.Contains(term) || (l.Email != null && l.Email.Contains(term)) || l.Phone.Contains(term));
        }

        if (status.HasValue)
        {
            query = query.Where(l => l.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.OwnerId))
        {
            query = query.Where(l => l.AssignedTo == filter.OwnerId);
        }

        if (filter.StartDate.HasValue)
        {
            query = query.Where(l => l.CreatedDate >= filter.StartDate.Value.ToUniversalTime());
        }

        if (filter.EndDate.HasValue)
        {
            var endOfDay = filter.EndDate.Value.Date.AddDays(1).AddTicks(-1).ToUniversalTime();
            query = query.Where(l => l.CreatedDate <= endOfDay);
        }

        query = query.OrderByDescending(l => l.CreatedDate);

        var total = await query.CountAsync();
        var items = await query.Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize).ToListAsync();

        return new PagedResult<Lead>
        {
            Items = items,
            TotalCount = total,
            PageNumber = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<PagedResult<FollowUp>> GetFollowUpReportAsync(string userId, string role, BaseReportFilter filter, FollowUpStatus? status = null)
    {
        var allowedUserIds = await _scopeService.GetAllowedUserIdsAsync(userId, role);
        var query = _scopeService.FilterFollowUps(
            _context.FollowUps.AsNoTracking()
                .Include(f => f.AssignedUser)
                .Include(f => f.Customer)
                .Include(f => f.Lead),
            allowedUserIds);

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.Trim();
            query = query.Where(f => f.Subject.Contains(term) || (f.Customer != null && f.Customer.CustomerName.Contains(term)));
        }

        if (status.HasValue)
        {
            query = query.Where(f => f.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.OwnerId))
        {
            query = query.Where(f => f.AssignedTo == filter.OwnerId);
        }

        if (filter.StartDate.HasValue)
        {
            query = query.Where(f => f.FollowUpDate >= filter.StartDate.Value.ToUniversalTime());
        }

        if (filter.EndDate.HasValue)
        {
            var endOfDay = filter.EndDate.Value.Date.AddDays(1).AddTicks(-1).ToUniversalTime();
            query = query.Where(f => f.FollowUpDate <= endOfDay);
        }

        query = query.OrderByDescending(f => f.FollowUpDate);

        var total = await query.CountAsync();
        var items = await query.Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize).ToListAsync();

        return new PagedResult<FollowUp>
        {
            Items = items,
            TotalCount = total,
            PageNumber = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<PagedResult<Opportunity>> GetOpportunityReportAsync(string userId, string role, BaseReportFilter filter, OpportunityStage? stage = null)
    {
        var allowedUserIds = await _scopeService.GetAllowedUserIdsAsync(userId, role);
        var query = _scopeService.FilterOpportunities(
            _context.Opportunities.AsNoTracking()
                .Include(o => o.Customer)
                .Include(o => o.AssignedUser),
            allowedUserIds);

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.Trim();
            query = query.Where(o => o.OpportunityName.Contains(term) || (o.Customer != null && o.Customer.CustomerName.Contains(term)));
        }

        if (stage.HasValue)
        {
            query = query.Where(o => o.Stage == stage.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.OwnerId))
        {
            query = query.Where(o => o.AssignedTo == filter.OwnerId);
        }

        if (filter.StartDate.HasValue)
        {
            query = query.Where(o => o.CreatedDate >= filter.StartDate.Value.ToUniversalTime());
        }

        if (filter.EndDate.HasValue)
        {
            var endOfDay = filter.EndDate.Value.Date.AddDays(1).AddTicks(-1).ToUniversalTime();
            query = query.Where(o => o.CreatedDate <= endOfDay);
        }

        query = query.OrderByDescending(o => o.CreatedDate);

        var total = await query.CountAsync();
        var items = await query.Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize).ToListAsync();

        return new PagedResult<Opportunity>
        {
            Items = items,
            TotalCount = total,
            PageNumber = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<PipelineReportViewModel> GetPipelineReportAsync(string userId, string role, BaseReportFilter filter)
    {
        var allowedUserIds = await _scopeService.GetAllowedUserIdsAsync(userId, role);
        var query = _scopeService.FilterOpportunities(
            _context.Opportunities.AsNoTracking()
                .Include(o => o.Customer)
                .Include(o => o.AssignedUser),
            allowedUserIds)
            .Where(o => o.Status == OpportunityStatus.Open); // Exclude Won and Lost

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.Trim();
            query = query.Where(o => o.OpportunityName.Contains(term) || (o.Customer != null && o.Customer.CustomerName.Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(filter.OwnerId))
        {
            query = query.Where(o => o.AssignedTo == filter.OwnerId);
        }

        var opps = await query.ToListAsync();

        var stageSummaries = opps
            .GroupBy(o => o.Stage)
            .Select(g => new StageSummaryItem
            {
                Stage = g.Key.ToString(),
                Count = g.Count(),
                TotalAmount = g.Sum(x => x.Amount),
                WeightedAmount = g.Sum(x => x.Amount * x.Probability / 100m)
            })
            .ToList();

        var ownerSummaries = opps
            .GroupBy(o => o.AssignedUser?.FullName ?? o.AssignedTo)
            .Select(g => new OwnerSummaryItem
            {
                OwnerName = g.Key,
                OpenCount = g.Count(),
                TotalAmount = g.Sum(x => x.Amount),
                WeightedAmount = g.Sum(x => x.Amount * x.Probability / 100m)
            })
            .ToList();

        var totalOpenCount = opps.Count;
        var totalOpenValue = opps.Sum(o => o.Amount);
        var totalWeighted = opps.Sum(o => o.Amount * o.Probability / 100m);

        var pagedItems = opps.Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize).ToList();

        return new PipelineReportViewModel
        {
            TotalOpenCount = totalOpenCount,
            TotalOpenValue = totalOpenValue,
            TotalWeightedValue = totalWeighted,
            StageSummaries = stageSummaries,
            OwnerSummaries = ownerSummaries,
            Opportunities = new PagedResult<Opportunity>
            {
                Items = pagedItems,
                TotalCount = totalOpenCount,
                PageNumber = filter.Page,
                PageSize = filter.PageSize
            }
        };
    }

    public async Task<ConversionReportViewModel> GetConversionReportAsync(string userId, string role, DateTime? startDate = null, DateTime? endDate = null)
    {
        var allowedUserIds = await _scopeService.GetAllowedUserIdsAsync(userId, role);
        var leadsQuery = _scopeService.FilterLeads(_context.Leads.AsNoTracking(), allowedUserIds);
        var oppsQuery = _scopeService.FilterOpportunities(_context.Opportunities.AsNoTracking(), allowedUserIds);

        if (startDate.HasValue)
        {
            leadsQuery = leadsQuery.Where(l => l.CreatedDate >= startDate.Value.ToUniversalTime());
            oppsQuery = oppsQuery.Where(o => o.CreatedDate >= startDate.Value.ToUniversalTime());
        }

        if (endDate.HasValue)
        {
            var endOfDay = endDate.Value.Date.AddDays(1).AddTicks(-1).ToUniversalTime();
            leadsQuery = leadsQuery.Where(l => l.CreatedDate <= endOfDay);
            oppsQuery = oppsQuery.Where(o => o.CreatedDate <= endOfDay);
        }

        var totalLeads = await leadsQuery.CountAsync();
        var convertedLeads = await leadsQuery.CountAsync(l => l.Status == LeadStatus.Converted);
        var unconvertedLeads = totalLeads - convertedLeads;
        var conversionRate = totalLeads > 0 ? Math.Round((double)convertedLeads / totalLeads * 100, 2) : 0;

        var wonOpps = await oppsQuery.CountAsync(o => o.Status == OpportunityStatus.Won || o.Stage == OpportunityStage.Won);
        var lostOpps = await oppsQuery.CountAsync(o => o.Status == OpportunityStatus.Lost || o.Stage == OpportunityStage.Lost);

        var wonRevenue = await oppsQuery.Where(o => o.Status == OpportunityStatus.Won || o.Stage == OpportunityStage.Won).SumAsync(o => o.Amount);
        var lostRevenue = await oppsQuery.Where(o => o.Status == OpportunityStatus.Lost || o.Stage == OpportunityStage.Lost).SumAsync(o => o.Amount);

        return new ConversionReportViewModel
        {
            TotalLeads = totalLeads,
            ConvertedLeads = convertedLeads,
            UnconvertedLeads = unconvertedLeads,
            ConversionRate = conversionRate,
            WonOpportunities = wonOpps,
            LostOpportunities = lostOpps,
            WonRevenue = wonRevenue,
            LostRevenue = lostRevenue
        };
    }

    public async Task<UserActivityReportViewModel> GetUserActivityReportAsync(string userId, string role, DateTime? startDate = null, DateTime? endDate = null)
    {
        var allowedUserIds = await _scopeService.GetAllowedUserIdsAsync(userId, role);
        var usersQuery = _context.Users.AsNoTracking().Where(u => u.IsActive);

        if (allowedUserIds != null)
        {
            usersQuery = usersQuery.Where(u => allowedUserIds.Contains(u.Id));
        }

        var users = await usersQuery.ToListAsync();

        var activitiesQuery = _scopeService.FilterActivities(_context.Activities.AsNoTracking(), allowedUserIds);
        var auditQuery = _context.AuditLogs.AsNoTracking().AsQueryable();

        if (startDate.HasValue)
        {
            activitiesQuery = activitiesQuery.Where(a => a.ActivityDate >= startDate.Value.ToUniversalTime());
            auditQuery = auditQuery.Where(a => a.CreatedDate >= startDate.Value.ToUniversalTime());
        }

        if (endDate.HasValue)
        {
            var endOfDay = endDate.Value.Date.AddDays(1).AddTicks(-1).ToUniversalTime();
            activitiesQuery = activitiesQuery.Where(a => a.ActivityDate <= endOfDay);
            auditQuery = auditQuery.Where(a => a.CreatedDate <= endOfDay);
        }

        var activities = await activitiesQuery.ToListAsync();
        var audits = await auditQuery.ToListAsync();

        var reportItems = new List<UserActivityReportItem>();

        foreach (var u in users)
        {
            var userActs = activities.Where(a => a.AssignedTo == u.Id).ToList();
            var userAudits = audits.Where(a => a.UserId == u.Id).Count();

            reportItems.Add(new UserActivityReportItem
            {
                UserId = u.Id,
                UserName = u.FullName,
                Role = role,
                TotalActivities = userActs.Count,
                CallsCount = userActs.Count(a => a.ActivityType == ActivityType.Call),
                MeetingsCount = userActs.Count(a => a.ActivityType == ActivityType.Meeting),
                EmailsCount = userActs.Count(a => a.ActivityType == ActivityType.Email),
                TasksCount = userActs.Count(a => a.ActivityType == ActivityType.Task),
                AuditActionsCount = userAudits
            });
        }

        return new UserActivityReportViewModel
        {
            UserActivities = reportItems.OrderByDescending(r => r.TotalActivities).ToList(),
            StartDate = startDate,
            EndDate = endDate
        };
    }

    // CSV Exports with Formula Injection Defense
    public async Task<byte[]> ExportCustomersCsvAsync(string userId, string role, BaseReportFilter filter)
    {
        filter.Page = 1;
        filter.PageSize = 10000;
        var result = await GetCustomerReportAsync(userId, role, filter);

        var sb = new StringBuilder();
        sb.AppendLine("Customer Code,Customer Name,Email,Phone,Company,City,State,Status,Owner,Created Date");

        foreach (var c in result.Items)
        {
            sb.AppendLine(string.Join(",",
                EscapeCsv(c.CustomerCode),
                EscapeCsv(c.CustomerName),
                EscapeCsv(c.Email),
                EscapeCsv(c.Phone),
                EscapeCsv(c.CompanyName ?? ""),
                EscapeCsv(c.City ?? ""),
                EscapeCsv(c.State ?? ""),
                EscapeCsv(c.Status.ToString()),
                EscapeCsv(c.Owner?.FullName ?? c.OwnerId),
                EscapeCsv(c.CreatedDate.ToString("yyyy-MM-dd HH:mm"))
            ));
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public async Task<byte[]> ExportLeadsCsvAsync(string userId, string role, BaseReportFilter filter)
    {
        filter.Page = 1;
        filter.PageSize = 10000;
        var result = await GetLeadReportAsync(userId, role, filter);

        var sb = new StringBuilder();
        sb.AppendLine("Lead Code,Lead Name,Email,Phone,Company,Source,Status,Priority,Expected Value,Assigned To,Created Date");

        foreach (var l in result.Items)
        {
            sb.AppendLine(string.Join(",",
                EscapeCsv(l.LeadCode),
                EscapeCsv(l.LeadName),
                EscapeCsv(l.Email ?? ""),
                EscapeCsv(l.Phone),
                EscapeCsv(l.CompanyName ?? ""),
                EscapeCsv(l.Source.ToString()),
                EscapeCsv(l.Status.ToString()),
                EscapeCsv(l.Priority.ToString()),
                EscapeCsv(l.ExpectedValue?.ToString("F2") ?? "0.00"),
                EscapeCsv(l.AssignedUser?.FullName ?? l.AssignedTo),
                EscapeCsv(l.CreatedDate.ToString("yyyy-MM-dd HH:mm"))
            ));
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public async Task<byte[]> ExportOpportunitiesCsvAsync(string userId, string role, BaseReportFilter filter)
    {
        filter.Page = 1;
        filter.PageSize = 10000;
        var result = await GetOpportunityReportAsync(userId, role, filter);

        var sb = new StringBuilder();
        sb.AppendLine("Opportunity Name,Customer,Amount,Stage,Probability,Weighted Pipeline,Close Date,Status,Assigned To,Created Date");

        foreach (var o in result.Items)
        {
            sb.AppendLine(string.Join(",",
                EscapeCsv(o.OpportunityName),
                EscapeCsv(o.Customer?.CustomerName ?? ""),
                EscapeCsv(o.Amount.ToString("F2")),
                EscapeCsv(o.Stage.ToString()),
                EscapeCsv(o.Probability.ToString() + "%"),
                EscapeCsv(o.WeightedPipeline.ToString("F2")),
                EscapeCsv(o.ExpectedCloseDate.ToString("yyyy-MM-dd")),
                EscapeCsv(o.Status.ToString()),
                EscapeCsv(o.AssignedUser?.FullName ?? o.AssignedTo),
                EscapeCsv(o.CreatedDate.ToString("yyyy-MM-dd HH:mm"))
            ));
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public async Task<byte[]> ExportFollowUpsCsvAsync(string userId, string role, BaseReportFilter filter)
    {
        filter.Page = 1;
        filter.PageSize = 10000;
        var result = await GetFollowUpReportAsync(userId, role, filter);

        var sb = new StringBuilder();
        sb.AppendLine("Date,Subject,Type,Status,Assigned To,Related Customer,Related Lead,Remarks");

        foreach (var f in result.Items)
        {
            sb.AppendLine(string.Join(",",
                EscapeCsv(f.FollowUpDate.ToString("yyyy-MM-dd HH:mm")),
                EscapeCsv(f.Subject),
                EscapeCsv(f.FollowUpType.ToString()),
                EscapeCsv(f.Status.ToString()),
                EscapeCsv(f.AssignedUser?.FullName ?? f.AssignedTo),
                EscapeCsv(f.Customer?.CustomerName ?? ""),
                EscapeCsv(f.Lead?.LeadName ?? ""),
                EscapeCsv(f.Remarks ?? "")
            ));
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public async Task<byte[]> ExportPipelineCsvAsync(string userId, string role, BaseReportFilter filter)
    {
        filter.Page = 1;
        filter.PageSize = 10000;
        var report = await GetPipelineReportAsync(userId, role, filter);

        var sb = new StringBuilder();
        sb.AppendLine("Opportunity Name,Customer,Stage,Amount,Probability,Weighted Value,Expected Close Date,Owner");

        foreach (var o in report.Opportunities.Items)
        {
            sb.AppendLine(string.Join(",",
                EscapeCsv(o.OpportunityName),
                EscapeCsv(o.Customer?.CustomerName ?? ""),
                EscapeCsv(o.Stage.ToString()),
                EscapeCsv(o.Amount.ToString("F2")),
                EscapeCsv(o.Probability.ToString() + "%"),
                EscapeCsv(o.WeightedPipeline.ToString("F2")),
                EscapeCsv(o.ExpectedCloseDate.ToString("yyyy-MM-dd")),
                EscapeCsv(o.AssignedUser?.FullName ?? o.AssignedTo)
            ));
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    /// <summary>
    /// Escapes CSV value and protects against CSV formula injection (DDE attacks)
    /// </summary>
    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "\"\"";

        // Defense against formula injection: prepend single quote if starts with dangerous chars
        var dangerousChars = new[] { '=', '+', '-', '@', '\t', '\r' };
        var trimmed = value.TrimStart();
        if (trimmed.Length > 0 && dangerousChars.Contains(trimmed[0]))
        {
            value = "'" + value;
        }

        return $"\"{value.Replace("\"", "\"\"")}\"";
    }
}

