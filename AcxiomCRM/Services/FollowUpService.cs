using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Models.Enums;
using AcxiomCRM.Models.ViewModels;

namespace AcxiomCRM.Services;

public interface IFollowUpService
{
    Task<PagedResult<FollowUp>> GetFollowUpsAsync(
        string userId,
        string role,
        string? searchTerm = null,
        FollowUpStatus? status = null,
        FollowUpType? type = null,
        string? assigneeId = null,
        string scope = "All", // All, Upcoming, Overdue, Today
        int page = 1,
        int pageSize = 10);

    Task<(int UpcomingCount, int OverdueCount)> GetCountsAsync(string userId, string role);
    Task<FollowUp?> GetFollowUpByIdAsync(int followUpId, string userId, string role);
    Task<(bool Success, string Message, FollowUp? FollowUp)> CreateFollowUpAsync(FollowUp followUp, string currentUserId, string role, string currentUserName);
    Task<(bool Success, string Message)> UpdateFollowUpAsync(FollowUp followUp, string currentUserId, string role, string currentUserName);
    Task<(bool Success, string Message)> CompleteFollowUpAsync(int followUpId, string? remarks, string currentUserId, string role, string currentUserName);
    Task<(bool Success, string Message)> MarkAsMissedAsync(int followUpId, string currentUserId, string role, string currentUserName);
    Task<(bool Success, string Message)> CancelFollowUpAsync(int followUpId, string currentUserId, string role, string currentUserName);
    Task<(bool Success, string Message)> RescheduleFollowUpAsync(int followUpId, DateTime newDate, string? newRemarks, string currentUserId, string role, string currentUserName);
}

public class FollowUpService : IFollowUpService
{
    private readonly ApplicationDbContext _context;
    private readonly IRecordScopeService _scopeService;
    private readonly IAuditService _auditService;

    public FollowUpService(
        ApplicationDbContext context,
        IRecordScopeService scopeService,
        IAuditService auditService)
    {
        _context = context;
        _scopeService = scopeService;
        _auditService = auditService;
    }

    public async Task<PagedResult<FollowUp>> GetFollowUpsAsync(
        string userId,
        string role,
        string? searchTerm = null,
        FollowUpStatus? status = null,
        FollowUpType? type = null,
        string? assigneeId = null,
        string scope = "All",
        int page = 1,
        int pageSize = 10)
    {
        var allowedUserIds = await _scopeService.GetAllowedUserIdsAsync(userId, role);
        var query = _scopeService.FilterFollowUps(
            _context.FollowUps
                .AsNoTracking()
                .Include(f => f.Customer)
                .Include(f => f.Lead)
                .Include(f => f.Opportunity)
                .Include(f => f.AssignedUser),
            allowedUserIds);

        var now = DateTime.UtcNow;

        if (scope == "Upcoming")
        {
            query = query.Where(f => f.Status == FollowUpStatus.Planned && f.FollowUpDate >= now);
        }
        else if (scope == "Overdue")
        {
            query = query.Where(f => f.Status == FollowUpStatus.Planned && f.FollowUpDate < now);
        }
        else if (scope == "Today")
        {
            var startOfDay = DateTime.UtcNow.Date;
            var endOfDay = startOfDay.AddDays(1).AddTicks(-1);
            query = query.Where(f => f.FollowUpDate >= startOfDay && f.FollowUpDate <= endOfDay);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            query = query.Where(f =>
                f.Subject.Contains(term) ||
                (f.Remarks != null && f.Remarks.Contains(term)) ||
                (f.Customer != null && f.Customer.CustomerName.Contains(term)) ||
                (f.Lead != null && f.Lead.LeadName.Contains(term)));
        }

        if (status.HasValue)
        {
            query = query.Where(f => f.Status == status.Value);
        }

        if (type.HasValue)
        {
            query = query.Where(f => f.FollowUpType == type.Value);
        }

        if (!string.IsNullOrWhiteSpace(assigneeId))
        {
            query = query.Where(f => f.AssignedTo == assigneeId);
        }

        query = query.OrderBy(f => f.FollowUpDate);

        var totalCount = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return new PagedResult<FollowUp>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = pageSize
        };
    }

    public async Task<(int UpcomingCount, int OverdueCount)> GetCountsAsync(string userId, string role)
    {
        var allowedUserIds = await _scopeService.GetAllowedUserIdsAsync(userId, role);
        var query = _scopeService.FilterFollowUps(_context.FollowUps.AsNoTracking(), allowedUserIds);

        var now = DateTime.UtcNow;
        var upcoming = await query.CountAsync(f => f.Status == FollowUpStatus.Planned && f.FollowUpDate >= now);
        var overdue = await query.CountAsync(f => f.Status == FollowUpStatus.Planned && f.FollowUpDate < now);

        return (upcoming, overdue);
    }

    public async Task<FollowUp?> GetFollowUpByIdAsync(int followUpId, string userId, string role)
    {
        if (!await _scopeService.CanAccessFollowUpAsync(followUpId, userId, role))
        {
            return null;
        }

        return await _context.FollowUps
            .Include(f => f.Customer)
            .Include(f => f.Lead)
            .Include(f => f.Opportunity)
            .Include(f => f.AssignedUser)
            .FirstOrDefaultAsync(f => f.FollowUpId == followUpId);
    }

    public async Task<(bool Success, string Message, FollowUp? FollowUp)> CreateFollowUpAsync(
        FollowUp followUp,
        string currentUserId,
        string role,
        string currentUserName)
    {
        if (string.IsNullOrWhiteSpace(followUp.Subject))
            return (false, "Subject is required.", null);

        // Validation rule: A new Planned Follow-Up cannot have a date earlier than today
        if (followUp.Status == FollowUpStatus.Planned && followUp.FollowUpDate.Date < DateTime.Today)
            return (false, "Follow-up date cannot be earlier than today.", null);

        // At least one relation or assigned
        if (string.IsNullOrWhiteSpace(followUp.AssignedTo))
        {
            followUp.AssignedTo = currentUserId;
        }
        else if (!await _scopeService.CanAssignToAsync(followUp.AssignedTo, currentUserId, role))
        {
            return (false, "You are not authorized to assign this follow-up to the selected user.", null);
        }

        followUp.CreatedDate = DateTime.UtcNow;
        followUp.CreatedBy = currentUserName;

        _context.FollowUps.Add(followUp);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            currentUserId,
            currentUserName,
            "Follow-Up Created",
            "FollowUp",
            followUp.FollowUpId.ToString(),
            null,
            $"Subject: {followUp.Subject}, Date: {followUp.FollowUpDate:g}, Type: {followUp.FollowUpType}",
            "Success");

        return (true, "Follow-up scheduled successfully.", followUp);
    }

    public async Task<(bool Success, string Message)> UpdateFollowUpAsync(
        FollowUp followUp,
        string currentUserId,
        string role,
        string currentUserName)
    {
        var existing = await _context.FollowUps.FindAsync(followUp.FollowUpId);
        if (existing == null)
            return (false, "Follow-up not found.");

        if (!await _scopeService.CanAccessFollowUpAsync(existing.FollowUpId, currentUserId, role))
            return (false, "You do not have permission to modify this follow-up.");

        if (existing.Status == FollowUpStatus.Planned && followUp.FollowUpDate.Date < DateTime.Today)
            return (false, "Follow-up date cannot be earlier than today.");

        if (existing.AssignedTo != followUp.AssignedTo && !await _scopeService.CanAssignToAsync(followUp.AssignedTo, currentUserId, role))
            return (false, "You are not authorized to assign this follow-up to the selected user.");

        existing.Subject = followUp.Subject;
        existing.FollowUpDate = followUp.FollowUpDate;
        existing.FollowUpType = followUp.FollowUpType;
        existing.Status = followUp.Status;
        existing.Remarks = followUp.Remarks;
        existing.CustomerId = followUp.CustomerId;
        existing.LeadId = followUp.LeadId;
        existing.OpportunityId = followUp.OpportunityId;
        existing.AssignedTo = followUp.AssignedTo;

        if (followUp.Status == FollowUpStatus.Completed && existing.CompletedDate == null)
        {
            existing.CompletedDate = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            currentUserId,
            currentUserName,
            "Follow-Up Updated",
            "FollowUp",
            existing.FollowUpId.ToString(),
            null,
            $"Subject: {existing.Subject}, Status: {existing.Status}",
            "Success");

        return (true, "Follow-up updated successfully.");
    }

    public async Task<(bool Success, string Message)> CompleteFollowUpAsync(
        int followUpId,
        string? remarks,
        string currentUserId,
        string role,
        string currentUserName)
    {
        var existing = await _context.FollowUps.FindAsync(followUpId);
        if (existing == null)
            return (false, "Follow-up not found.");

        if (!await _scopeService.CanAccessFollowUpAsync(followUpId, currentUserId, role))
            return (false, "You do not have permission to update this follow-up.");

        var oldStatus = existing.Status;
        existing.Status = FollowUpStatus.Completed;
        existing.CompletedDate = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(remarks))
        {
            existing.Remarks = string.IsNullOrWhiteSpace(existing.Remarks)
                ? remarks
                : $"{existing.Remarks}\n[Completed Notes]: {remarks}";
        }

        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            currentUserId,
            currentUserName,
            "Follow-Up Completed",
            "FollowUp",
            followUpId.ToString(),
            $"Status: {oldStatus}",
            "Status: Completed",
            "Success");

        return (true, "Follow-up marked as completed.");
    }

    public async Task<(bool Success, string Message)> MarkAsMissedAsync(
        int followUpId,
        string currentUserId,
        string role,
        string currentUserName)
    {
        var existing = await _context.FollowUps.FindAsync(followUpId);
        if (existing == null)
            return (false, "Follow-up not found.");

        if (!await _scopeService.CanAccessFollowUpAsync(followUpId, currentUserId, role))
            return (false, "You do not have permission to update this follow-up.");

        var oldStatus = existing.Status;
        existing.Status = FollowUpStatus.Missed;

        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            currentUserId,
            currentUserName,
            "Follow-Up Missed",
            "FollowUp",
            followUpId.ToString(),
            $"Status: {oldStatus}",
            "Status: Missed",
            "Success");

        return (true, "Follow-up marked as missed.");
    }

    public async Task<(bool Success, string Message)> CancelFollowUpAsync(
        int followUpId,
        string currentUserId,
        string role,
        string currentUserName)
    {
        var existing = await _context.FollowUps.FindAsync(followUpId);
        if (existing == null)
            return (false, "Follow-up not found.");

        if (!await _scopeService.CanAccessFollowUpAsync(followUpId, currentUserId, role))
            return (false, "You do not have permission to cancel this follow-up.");

        var oldStatus = existing.Status;
        existing.Status = FollowUpStatus.Cancelled;

        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            currentUserId,
            currentUserName,
            "Follow-Up Cancelled",
            "FollowUp",
            followUpId.ToString(),
            $"Status: {oldStatus}",
            "Status: Cancelled",
            "Success");

        return (true, "Follow-up cancelled.");
    }

    public async Task<(bool Success, string Message)> RescheduleFollowUpAsync(
        int followUpId,
        DateTime newDate,
        string? newRemarks,
        string currentUserId,
        string role,
        string currentUserName)
    {
        var existing = await _context.FollowUps.FindAsync(followUpId);
        if (existing == null)
            return (false, "Follow-up not found.");

        if (!await _scopeService.CanAccessFollowUpAsync(followUpId, currentUserId, role))
            return (false, "You do not have permission to reschedule this follow-up.");

        if (newDate.Date < DateTime.Today)
            return (false, "Rescheduled follow-up date cannot be earlier than today.");

        var oldDate = existing.FollowUpDate;
        existing.FollowUpDate = newDate;
        existing.Status = FollowUpStatus.Planned;
        if (!string.IsNullOrWhiteSpace(newRemarks))
        {
            existing.Remarks = string.IsNullOrWhiteSpace(existing.Remarks)
                ? newRemarks
                : $"{existing.Remarks}\n[Rescheduled to {newDate:g}]: {newRemarks}";
        }

        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            currentUserId,
            currentUserName,
            "Follow-Up Rescheduled",
            "FollowUp",
            followUpId.ToString(),
            $"Date: {oldDate:g}",
            $"Date: {newDate:g}",
            "Success");

        return (true, "Follow-up rescheduled successfully.");
    }
}

