using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Data;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Models.Enums;
using AcxiomCRM.Models.ViewModels;

namespace AcxiomCRM.Services;

public interface IOpportunityService
{
    Task<PagedResult<Opportunity>> GetOpportunitiesAsync(
        string userId,
        string role,
        string? searchTerm = null,
        OpportunityStage? stage = null,
        OpportunityStatus? status = null,
        string? assigneeId = null,
        string sortBy = "DateDesc",
        int page = 1,
        int pageSize = 10);

    Task<Opportunity?> GetOpportunityByIdAsync(int opportunityId, string userId, string role, bool includeRelated = false);
    Task<(bool Success, string Message, Opportunity? Opportunity)> CreateOpportunityAsync(Opportunity opportunity, string currentUserId, string role, string currentUserName);
    Task<(bool Success, string Message)> UpdateOpportunityAsync(Opportunity opportunity, string currentUserId, string role, string currentUserName);
    Task<(bool Success, string Message)> ChangeStageAsync(int opportunityId, OpportunityStage newStage, string currentUserId, string role, string currentUserName);
    Task<(bool Success, string Message)> DeleteOrCloseAsync(int opportunityId, OpportunityStatus newStatus, string currentUserId, string role, string currentUserName);
}

public class OpportunityService : IOpportunityService
{
    private readonly ApplicationDbContext _context;
    private readonly IRecordScopeService _scopeService;
    private readonly IAuditService _auditService;

    public OpportunityService(
        ApplicationDbContext context,
        IRecordScopeService scopeService,
        IAuditService auditService)
    {
        _context = context;
        _scopeService = scopeService;
        _auditService = auditService;
    }

    public async Task<PagedResult<Opportunity>> GetOpportunitiesAsync(
        string userId,
        string role,
        string? searchTerm = null,
        OpportunityStage? stage = null,
        OpportunityStatus? status = null,
        string? assigneeId = null,
        string sortBy = "DateDesc",
        int page = 1,
        int pageSize = 10)
    {
        var allowedUserIds = await _scopeService.GetAllowedUserIdsAsync(userId, role);
        var query = _scopeService.FilterOpportunities(
            _context.Opportunities
                .AsNoTracking()
                .Include(o => o.Customer)
                .Include(o => o.Lead)
                .Include(o => o.AssignedUser),
            allowedUserIds);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            query = query.Where(o =>
                o.OpportunityName.Contains(term) ||
                (o.Customer != null && o.Customer.CustomerName.Contains(term)) ||
                (o.Notes != null && o.Notes.Contains(term)));
        }

        if (stage.HasValue)
        {
            query = query.Where(o => o.Stage == stage.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(o => o.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(assigneeId))
        {
            query = query.Where(o => o.AssignedTo == assigneeId);
        }

        query = sortBy switch
        {
            "NameAsc" => query.OrderBy(o => o.OpportunityName),
            "NameDesc" => query.OrderByDescending(o => o.OpportunityName),
            "AmountDesc" => query.OrderByDescending(o => o.Amount),
            "AmountAsc" => query.OrderBy(o => o.Amount),
            "CloseDateAsc" => query.OrderBy(o => o.ExpectedCloseDate),
            "DateAsc" => query.OrderBy(o => o.CreatedDate),
            _ => query.OrderByDescending(o => o.CreatedDate)
        };

        var totalCount = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return new PagedResult<Opportunity>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = pageSize
        };
    }

    public async Task<Opportunity?> GetOpportunityByIdAsync(int opportunityId, string userId, string role, bool includeRelated = false)
    {
        if (!await _scopeService.CanAccessOpportunityAsync(opportunityId, userId, role))
        {
            return null;
        }

        var query = _context.Opportunities.AsQueryable();

        if (includeRelated)
        {
            query = query
                .Include(o => o.Customer)
                .Include(o => o.Lead)
                .Include(o => o.AssignedUser)
                .Include(o => o.FollowUps)
                .Include(o => o.Activities);
        }
        else
        {
            query = query.Include(o => o.Customer).Include(o => o.AssignedUser);
        }

        return await query.FirstOrDefaultAsync(o => o.OpportunityId == opportunityId);
    }

    public async Task<(bool Success, string Message, Opportunity? Opportunity)> CreateOpportunityAsync(
        Opportunity opportunity,
        string currentUserId,
        string role,
        string currentUserName)
    {
        if (string.IsNullOrWhiteSpace(opportunity.OpportunityName))
            return (false, "Opportunity Name is required.", null);

        if (opportunity.Amount <= 0)
            return (false, "Opportunity Amount must be greater than 0.", null);

        if (opportunity.Probability < 0 || opportunity.Probability > 100)
            return (false, "Probability must be between 0 and 100.", null);

        if (opportunity.ExpectedCloseDate.Date < DateTime.Today && opportunity.Status == OpportunityStatus.Open)
            return (false, "Expected Close Date cannot be in the past.", null);

        // Verify Customer exists and is accessible
        if (!await _scopeService.CanAccessCustomerAsync(opportunity.CustomerId, currentUserId, role))
            return (false, "Specified customer does not exist or you do not have permission to access it.", null);

        // Verify assignment
        if (string.IsNullOrWhiteSpace(opportunity.AssignedTo))
        {
            opportunity.AssignedTo = currentUserId;
        }
        else if (!await _scopeService.CanAssignToAsync(opportunity.AssignedTo, currentUserId, role))
        {
            return (false, "You are not authorized to assign this opportunity to the specified user.", null);
        }

        // Synchronize stage and status
        SyncStageAndStatus(opportunity);

        opportunity.CreatedDate = DateTime.UtcNow;
        opportunity.CreatedBy = currentUserName;

        _context.Opportunities.Add(opportunity);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            currentUserId,
            currentUserName,
            "Opportunity Created",
            "Opportunity",
            opportunity.OpportunityId.ToString(),
            null,
            $"Name: {opportunity.OpportunityName}, Amount: {opportunity.Amount:C}, Stage: {opportunity.Stage}",
            "Success");

        return (true, "Opportunity created successfully.", opportunity);
    }

    public async Task<(bool Success, string Message)> UpdateOpportunityAsync(
        Opportunity opportunity,
        string currentUserId,
        string role,
        string currentUserName)
    {
        var existing = await _context.Opportunities.FindAsync(opportunity.OpportunityId);
        if (existing == null)
            return (false, "Opportunity not found.");

        if (!await _scopeService.CanAccessOpportunityAsync(existing.OpportunityId, currentUserId, role))
            return (false, "You do not have permission to modify this opportunity.");

        if (opportunity.Amount <= 0)
            return (false, "Opportunity Amount must be greater than 0.");

        if (opportunity.Probability < 0 || opportunity.Probability > 100)
            return (false, "Probability must be between 0 and 100.");

        if (opportunity.ExpectedCloseDate.Date < DateTime.Today && opportunity.Stage is not (OpportunityStage.Won or OpportunityStage.Lost))
            return (false, "Expected Close Date cannot be in the past for active opportunities.");

        if (existing.CustomerId != opportunity.CustomerId && !await _scopeService.CanAccessCustomerAsync(opportunity.CustomerId, currentUserId, role))
            return (false, "Specified customer does not exist or is unauthorized.");

        if (existing.AssignedTo != opportunity.AssignedTo && !await _scopeService.CanAssignToAsync(opportunity.AssignedTo, currentUserId, role))
            return (false, "You are not authorized to assign this opportunity to the specified user.");

        var oldSummary = $"Stage: {existing.Stage}, Amount: {existing.Amount}, Probability: {existing.Probability}%";

        existing.OpportunityName = opportunity.OpportunityName;
        existing.CustomerId = opportunity.CustomerId;
        existing.LeadId = opportunity.LeadId;
        existing.Amount = opportunity.Amount;
        existing.Stage = opportunity.Stage;
        existing.Probability = opportunity.Probability;
        existing.ExpectedCloseDate = opportunity.ExpectedCloseDate;
        existing.Source = opportunity.Source;
        existing.Notes = opportunity.Notes;
        existing.AssignedTo = opportunity.AssignedTo;
        existing.ModifiedDate = DateTime.UtcNow;
        existing.ModifiedBy = currentUserName;

        SyncStageAndStatus(existing);

        await _context.SaveChangesAsync();

        var newSummary = $"Stage: {existing.Stage}, Amount: {existing.Amount}, Probability: {existing.Probability}%";

        await _auditService.LogAsync(
            currentUserId,
            currentUserName,
            "Opportunity Updated",
            "Opportunity",
            existing.OpportunityId.ToString(),
            oldSummary,
            newSummary,
            "Success");

        return (true, "Opportunity updated successfully.");
    }

    public async Task<(bool Success, string Message)> ChangeStageAsync(
        int opportunityId,
        OpportunityStage newStage,
        string currentUserId,
        string role,
        string currentUserName)
    {
        var existing = await _context.Opportunities.FindAsync(opportunityId);
        if (existing == null)
            return (false, "Opportunity not found.");

        if (!await _scopeService.CanAccessOpportunityAsync(opportunityId, currentUserId, role))
            return (false, "You do not have permission to modify this opportunity.");

        var oldStage = existing.Stage;
        existing.Stage = newStage;
        SyncStageAndStatus(existing);
        existing.ModifiedDate = DateTime.UtcNow;
        existing.ModifiedBy = currentUserName;

        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            currentUserId,
            currentUserName,
            "Opportunity Stage Changed",
            "Opportunity",
            opportunityId.ToString(),
            $"Stage: {oldStage}",
            $"Stage: {newStage}, Status: {existing.Status}",
            "Success");

        return (true, $"Opportunity stage updated to {newStage}.");
    }

    public async Task<(bool Success, string Message)> DeleteOrCloseAsync(
        int opportunityId,
        OpportunityStatus newStatus,
        string currentUserId,
        string role,
        string currentUserName)
    {
        var existing = await _context.Opportunities.FindAsync(opportunityId);
        if (existing == null)
            return (false, "Opportunity not found.");

        if (!await _scopeService.CanAccessOpportunityAsync(opportunityId, currentUserId, role))
            return (false, "You do not have permission to update this opportunity.");

        existing.Status = newStatus;
        if (newStatus == OpportunityStatus.Won)
        {
            existing.Stage = OpportunityStage.Won;
            existing.Probability = 100;
        }
        else if (newStatus == OpportunityStatus.Lost)
        {
            existing.Stage = OpportunityStage.Lost;
            existing.Probability = 0;
        }

        existing.ModifiedDate = DateTime.UtcNow;
        existing.ModifiedBy = currentUserName;

        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            currentUserId,
            currentUserName,
            "Opportunity Status Changed",
            "Opportunity",
            opportunityId.ToString(),
            null,
            $"Status: {newStatus}",
            "Success");

        return (true, $"Opportunity status changed to {newStatus}.");
    }

    private static void SyncStageAndStatus(Opportunity opportunity)
    {
        switch (opportunity.Stage)
        {
            case OpportunityStage.Won:
                opportunity.Status = OpportunityStatus.Won;
                opportunity.Probability = 100;
                break;
            case OpportunityStage.Lost:
                opportunity.Status = OpportunityStatus.Lost;
                opportunity.Probability = 0;
                break;
            default:
                opportunity.Status = OpportunityStatus.Open;
                if (opportunity.Probability == 0 || opportunity.Probability == 100)
                {
                    opportunity.Probability = opportunity.Stage switch
                    {
                        OpportunityStage.Qualification => 20,
                        OpportunityStage.Proposal => 50,
                        OpportunityStage.Negotiation => 80,
                        _ => 30
                    };
                }
                break;
        }
    }
}

