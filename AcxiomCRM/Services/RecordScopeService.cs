using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Authorization;
using AcxiomCRM.Data;
using AcxiomCRM.Models.Entities;

namespace AcxiomCRM.Services;

public interface IRecordScopeService
{
    Task<List<string>?> GetAllowedUserIdsAsync(string userId, string role);
    IQueryable<Customer> FilterCustomers(IQueryable<Customer> query, List<string>? allowedUserIds);
    IQueryable<Lead> FilterLeads(IQueryable<Lead> query, List<string>? allowedUserIds);
    IQueryable<Opportunity> FilterOpportunities(IQueryable<Opportunity> query, List<string>? allowedUserIds);
    IQueryable<FollowUp> FilterFollowUps(IQueryable<FollowUp> query, List<string>? allowedUserIds);
    IQueryable<Activity> FilterActivities(IQueryable<Activity> query, List<string>? allowedUserIds);

    Task<bool> CanAccessCustomerAsync(int customerId, string userId, string role);
    Task<bool> CanAccessLeadAsync(int leadId, string userId, string role);
    Task<bool> CanAccessOpportunityAsync(int opportunityId, string userId, string role);
    Task<bool> CanAccessFollowUpAsync(int followUpId, string userId, string role);
    Task<bool> CanAccessActivityAsync(int activityId, string userId, string role);
    Task<bool> CanAssignToAsync(string targetUserId, string currentUserId, string role);
}

public class RecordScopeService : IRecordScopeService
{
    private readonly ApplicationDbContext _context;

    public RecordScopeService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<string>?> GetAllowedUserIdsAsync(string userId, string role)
    {
        if (role == RoleConstants.Admin)
        {
            return null; // Null signifies unrestricted access
        }

        if (role == RoleConstants.Manager)
        {
            var subordinateIds = await _context.Users
                .AsNoTracking()
                .Where(u => u.ManagerId == userId && u.IsActive)
                .Select(u => u.Id)
                .ToListAsync();

            subordinateIds.Add(userId);
            return subordinateIds;
        }

        // Sales Executive can only access their own records
        return new List<string> { userId };
    }

    public IQueryable<Customer> FilterCustomers(IQueryable<Customer> query, List<string>? allowedUserIds)
    {
        if (allowedUserIds == null)
            return query;

        return query.Where(c => allowedUserIds.Contains(c.OwnerId));
    }

    public IQueryable<Lead> FilterLeads(IQueryable<Lead> query, List<string>? allowedUserIds)
    {
        if (allowedUserIds == null)
            return query;

        return query.Where(l => allowedUserIds.Contains(l.AssignedTo));
    }

    public IQueryable<Opportunity> FilterOpportunities(IQueryable<Opportunity> query, List<string>? allowedUserIds)
    {
        if (allowedUserIds == null)
            return query;

        return query.Where(o => allowedUserIds.Contains(o.AssignedTo));
    }

    public IQueryable<FollowUp> FilterFollowUps(IQueryable<FollowUp> query, List<string>? allowedUserIds)
    {
        if (allowedUserIds == null)
            return query;

        return query.Where(f => allowedUserIds.Contains(f.AssignedTo));
    }

    public IQueryable<Activity> FilterActivities(IQueryable<Activity> query, List<string>? allowedUserIds)
    {
        if (allowedUserIds == null)
            return query;

        return query.Where(a => allowedUserIds.Contains(a.AssignedTo));
    }

    public async Task<bool> CanAccessCustomerAsync(int customerId, string userId, string role)
    {
        if (role == RoleConstants.Admin) return true;
        var allowedIds = await GetAllowedUserIdsAsync(userId, role);
        return await _context.Customers.AnyAsync(c => c.CustomerId == customerId && allowedIds!.Contains(c.OwnerId));
    }

    public async Task<bool> CanAccessLeadAsync(int leadId, string userId, string role)
    {
        if (role == RoleConstants.Admin) return true;
        var allowedIds = await GetAllowedUserIdsAsync(userId, role);
        return await _context.Leads.AnyAsync(l => l.LeadId == leadId && allowedIds!.Contains(l.AssignedTo));
    }

    public async Task<bool> CanAccessOpportunityAsync(int opportunityId, string userId, string role)
    {
        if (role == RoleConstants.Admin) return true;
        var allowedIds = await GetAllowedUserIdsAsync(userId, role);
        return await _context.Opportunities.AnyAsync(o => o.OpportunityId == opportunityId && allowedIds!.Contains(o.AssignedTo));
    }

    public async Task<bool> CanAccessFollowUpAsync(int followUpId, string userId, string role)
    {
        if (role == RoleConstants.Admin) return true;
        var allowedIds = await GetAllowedUserIdsAsync(userId, role);
        return await _context.FollowUps.AnyAsync(f => f.FollowUpId == followUpId && allowedIds!.Contains(f.AssignedTo));
    }

    public async Task<bool> CanAccessActivityAsync(int activityId, string userId, string role)
    {
        if (role == RoleConstants.Admin) return true;
        var allowedIds = await GetAllowedUserIdsAsync(userId, role);
        return await _context.Activities.AnyAsync(a => a.ActivityId == activityId && allowedIds!.Contains(a.AssignedTo));
    }

    public async Task<bool> CanAssignToAsync(string targetUserId, string currentUserId, string role)
    {
        if (role == RoleConstants.Admin)
        {
            return await _context.Users.AnyAsync(u => u.Id == targetUserId && u.IsActive);
        }

        if (role == RoleConstants.Manager)
        {
            var allowedIds = await GetAllowedUserIdsAsync(currentUserId, role);
            return allowedIds != null && allowedIds.Contains(targetUserId);
        }

        // Sales executive can only assign to self
        return targetUserId == currentUserId;
    }
}

