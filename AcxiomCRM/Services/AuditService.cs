using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.Models.Entities;

namespace AcxiomCRM.Services;

public interface IAuditService
{
    Task LogAsync(
        string? userId,
        string? userName,
        string action,
        string entityName,
        string? recordId,
        string? oldValue = null,
        string? newValue = null,
        string result = "Success",
        string? ipAddress = null,
        string? details = null);

    Task<(List<AuditLog> Items, int TotalCount)> GetAuditLogsAsync(
        string? search = null,
        string? module = null,
        string? action = null,
        string? result = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int page = 1,
        int pageSize = 20);
}

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AuditService> _logger;

    public AuditService(ApplicationDbContext context, ILogger<AuditService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task LogAsync(
        string? userId,
        string? userName,
        string action,
        string entityName,
        string? recordId,
        string? oldValue = null,
        string? newValue = null,
        string result = "Success",
        string? ipAddress = null,
        string? details = null)
    {
        try
        {
            var log = new AuditLog
            {
                UserId = userId,
                UserName = userName,
                Action = action,
                EntityName = entityName,
                RecordId = recordId,
                OldValue = Sanitize(oldValue),
                NewValue = Sanitize(newValue),
                Result = result,
                CreatedDate = DateTime.UtcNow,
                IpAddress = ipAddress,
                Details = Sanitize(details)
            };

            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Never break business transactions because of logging error, but log diagnostic
            _logger.LogError(ex, "Failed to record audit log for action {Action} on {EntityName}", action, entityName);
        }
    }

    public async Task<(List<AuditLog> Items, int TotalCount)> GetAuditLogsAsync(
        string? search = null,
        string? module = null,
        string? action = null,
        string? result = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int page = 1,
        int pageSize = 20)
    {
        var query = _context.AuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(a =>
                (a.UserName != null && a.UserName.Contains(s)) ||
                (a.Details != null && a.Details.Contains(s)) ||
                (a.RecordId != null && a.RecordId.Contains(s)));
        }

        if (!string.IsNullOrWhiteSpace(module))
        {
            query = query.Where(a => a.EntityName == module);
        }

        if (!string.IsNullOrWhiteSpace(action))
        {
            query = query.Where(a => a.Action.Contains(action));
        }

        if (!string.IsNullOrWhiteSpace(result))
        {
            query = query.Where(a => a.Result == result);
        }

        if (startDate.HasValue)
        {
            query = query.Where(a => a.CreatedDate >= startDate.Value.ToUniversalTime());
        }

        if (endDate.HasValue)
        {
            var endOfDay = endDate.Value.Date.AddDays(1).AddTicks(-1).ToUniversalTime();
            query = query.Where(a => a.CreatedDate <= endOfDay);
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(a => a.CreatedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    private static string? Sanitize(string? text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        // Strip out passwords or sensitive keys if accidentally present
        if (text.Contains("password", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("token", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("secret", StringComparison.OrdinalIgnoreCase))
        {
            return "[REDACTED]";
        }
        return text.Length > 3000 ? text.Substring(0, 3000) + "..." : text;
    }
}

