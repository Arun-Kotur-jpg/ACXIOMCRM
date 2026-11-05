using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Models.Enums;
using AcxiomCRM.Models.ViewModels;

namespace AcxiomCRM.Services;

public interface IActivityService
{
    Task<PagedResult<Activity>> GetActivitiesAsync(
        string userId,
        string role,
        string? searchTerm = null,
        ActivityType? type = null,
        ActivityStatus? status = null,
        string? assigneeId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int page = 1,
        int pageSize = 10);

    Task<Activity?> GetActivityByIdAsync(int activityId, string userId, string role);
    Task<(bool Success, string Message, Activity? Activity)> CreateActivityAsync(Activity activity, string currentUserId, string role, string currentUserName);
    Task<(bool Success, string Message)> UpdateActivityAsync(Activity activity, string currentUserId, string role, string currentUserName);
    Task<(bool Success, string Message)> CompleteActivityAsync(int activityId, string currentUserId, string role, string currentUserName);
}

public class ActivityService : IActivityService
{
    private readonly ApplicationDbContext _context;
    private readonly IRecordScopeService _scopeService;
    private readonly IAuditService _auditService;

    public ActivityService(
        ApplicationDbContext context,
        IRecordScopeService scopeService,
        IAuditService auditService)
    {
        _context = context;
        _scopeService = scopeService;
        _auditService = auditService;
    }

    public async Task<PagedResult<Activity>> GetActivitiesAsync(
        string userId,
        string role,
        string? searchTerm = null,
        ActivityType? type = null,
        ActivityStatus? status = null,
        string? assigneeId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int page = 1,
        int pageSize = 10)
    {
        var allowedUserIds = await _scopeService.GetAllowedUserIdsAsync(userId, role);
        var query = _scopeService.FilterActivities(
            _context.Activities
                .AsNoTracking()
                .Include(a => a.Customer)
                .Include(a => a.Lead)
                .Include(a => a.Opportunity)
                .Include(a => a.AssignedUser),
            allowedUserIds);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            query = query.Where(a =>
                a.Subject.Contains(term) ||
                (a.Description != null && a.Description.Contains(term)) ||
                (a.Customer != null && a.Customer.CustomerName.Contains(term)) ||
                (a.Lead != null && a.Lead.LeadName.Contains(term)));
        }

        if (type.HasValue)
        {
            query = query.Where(a => a.ActivityType == type.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(a => a.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(assigneeId))
        {
            query = query.Where(a => a.AssignedTo == assigneeId);
        }

        if (startDate.HasValue)
        {
            query = query.Where(a => a.ActivityDate >= startDate.Value.ToUniversalTime());
        }

        if (endDate.HasValue)
        {
            var endOfDay = endDate.Value.Date.AddDays(1).AddTicks(-1).ToUniversalTime();
            query = query.Where(a => a.ActivityDate <= endOfDay);
        }

        query = query.OrderByDescending(a => a.ActivityDate);

        var totalCount = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return new PagedResult<Activity>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = pageSize
        };
    }

    public async Task<Activity?> GetActivityByIdAsync(int activityId, string userId, string role)
    {
        if (!await _scopeService.CanAccessActivityAsync(activityId, userId, role))
        {
            return null;
        }

        return await _context.Activities
            .Include(a => a.Customer)
            .Include(a => a.Lead)
            .Include(a => a.Opportunity)
            .Include(a => a.AssignedUser)
            .FirstOrDefaultAsync(a => a.ActivityId == activityId);
    }

    public async Task<(bool Success, string Message, Activity? Activity)> CreateActivityAsync(
        Activity activity,
        string currentUserId,
        string role,
        string currentUserName)
    {
        if (string.IsNullOrWhiteSpace(activity.Subject))
            return (false, "Subject is required.", null);

        if (string.IsNullOrWhiteSpace(activity.AssignedTo))
        {
            activity.AssignedTo = currentUserId;
        }
        else if (!await _scopeService.CanAssignToAsync(activity.AssignedTo, currentUserId, role))
        {
            return (false, "You are not authorized to assign this activity to the selected user.", null);
        }

        activity.CreatedDate = DateTime.UtcNow;
        activity.CreatedBy = currentUserName;

        _context.Activities.Add(activity);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            currentUserId,
            currentUserName,
            "Activity Created",
            "Activity",
            activity.ActivityId.ToString(),
            null,
            $"Type: {activity.ActivityType}, Subject: {activity.Subject}",
            "Success");

        return (true, "Activity recorded successfully.", activity);
    }

    public async Task<(bool Success, string Message)> UpdateActivityAsync(
        Activity activity,
        string currentUserId,
        string role,
        string currentUserName)
    {
        var existing = await _context.Activities.FindAsync(activity.ActivityId);
        if (existing == null)
            return (false, "Activity not found.");

        if (!await _scopeService.CanAccessActivityAsync(existing.ActivityId, currentUserId, role))
            return (false, "You do not have permission to modify this activity.");

        if (existing.AssignedTo != activity.AssignedTo && !await _scopeService.CanAssignToAsync(activity.AssignedTo, currentUserId, role))
            return (false, "You are not authorized to assign this activity to the selected user.");

        existing.Subject = activity.Subject;
        existing.Description = activity.Description;
        existing.ActivityType = activity.ActivityType;
        existing.ActivityDate = activity.ActivityDate;
        existing.Status = activity.Status;
        existing.CustomerId = activity.CustomerId;
        existing.LeadId = activity.LeadId;
        existing.OpportunityId = activity.OpportunityId;
        existing.AssignedTo = activity.AssignedTo;

        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            currentUserId,
            currentUserName,
            "Activity Updated",
            "Activity",
            existing.ActivityId.ToString(),
            null,
            $"Subject: {existing.Subject}, Status: {existing.Status}",
            "Success");

        return (true, "Activity updated successfully.");
    }

    public async Task<(bool Success, string Message)> CompleteActivityAsync(
        int activityId,
        string currentUserId,
        string role,
        string currentUserName)
    {
        var existing = await _context.Activities.FindAsync(activityId);
        if (existing == null)
            return (false, "Activity not found.");

        if (!await _scopeService.CanAccessActivityAsync(activityId, currentUserId, role))
            return (false, "You do not have permission to update this activity.");

        existing.Status = ActivityStatus.Completed;
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            currentUserId,
            currentUserName,
            "Activity Completed",
            "Activity",
            activityId.ToString(),
            null,
            "Status: Completed",
            "Success");

        return (true, "Activity marked as completed.");
    }
}

