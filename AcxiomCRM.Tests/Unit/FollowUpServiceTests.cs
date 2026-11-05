using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Authorization;
using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Models.Enums;
using AcxiomCRM.Services;
using Xunit;

namespace AcxiomCRM.Tests.Unit;

public class FollowUpServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _context;
    private readonly IRecordScopeService _scopeService;
    private readonly IAuditService _auditService;
    private readonly FollowUpService _followUpService;

    public FollowUpServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();

        _context.Users.Add(new ApplicationUser
        {
            Id = "rep-1",
            UserName = "rep1@test.com",
            Email = "rep1@test.com",
            FullName = "Sales Rep 1",
            IsActive = true
        });
        _context.SaveChanges();

        _scopeService = new RecordScopeService(_context);
        _auditService = new FakeAuditService();
        _followUpService = new FollowUpService(_context, _scopeService, _auditService);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task CreateFollowUpAsync_DateInPast_FailsValidation()
    {
        var followUp = new FollowUp
        {
            Subject = "Demo Call",
            FollowUpDate = DateTime.Today.AddDays(-2), // 2 days in the past
            FollowUpType = FollowUpType.Call,
            Status = FollowUpStatus.Planned,
            AssignedTo = "rep-1"
        };

        var (success, message, _) = await _followUpService.CreateFollowUpAsync(
            followUp, "rep-1", RoleConstants.SalesExecutive, "Sales Rep");

        Assert.False(success);
        Assert.Contains("earlier than today", message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateFollowUpAsync_FutureDate_Succeeds()
    {
        var followUp = new FollowUp
        {
            Subject = "Product Demo Pitch",
            FollowUpDate = DateTime.Today.AddDays(3),
            FollowUpType = FollowUpType.Meeting,
            Status = FollowUpStatus.Planned,
            AssignedTo = "rep-1"
        };

        var (success, message, created) = await _followUpService.CreateFollowUpAsync(
            followUp, "rep-1", RoleConstants.SalesExecutive, "Sales Rep");

        Assert.True(success);
        Assert.NotNull(created);
        Assert.Equal(FollowUpStatus.Planned, created.Status);
    }

    [Fact]
    public async Task CompleteFollowUpAsync_ValidPlanned_SetsStatusCompletedAndCompletedDate()
    {
        var followUp = new FollowUp
        {
            Subject = "Follow-up Call",
            FollowUpDate = DateTime.Today.AddDays(1),
            FollowUpType = FollowUpType.Call,
            Status = FollowUpStatus.Planned,
            AssignedTo = "rep-1",
            CreatedBy = "system"
        };
        _context.FollowUps.Add(followUp);
        await _context.SaveChangesAsync();

        var (success, message) = await _followUpService.CompleteFollowUpAsync(
            followUp.FollowUpId, "Client agreed to proposal", "rep-1", RoleConstants.SalesExecutive, "Sales Rep");

        Assert.True(success);
        var updated = await _context.FollowUps.FindAsync(followUp.FollowUpId);
        Assert.Equal(FollowUpStatus.Completed, updated!.Status);
        Assert.NotNull(updated.CompletedDate);
        Assert.Contains("agreed to proposal", updated.Remarks);
    }

    [Fact]
    public async Task RescheduleFollowUpAsync_UpdatesDateAndPreservesPlannedStatus()
    {
        var followUp = new FollowUp
        {
            Subject = "Contract Review",
            FollowUpDate = DateTime.Today.AddDays(1),
            FollowUpType = FollowUpType.Meeting,
            Status = FollowUpStatus.Planned,
            AssignedTo = "rep-1",
            CreatedBy = "system"
        };
        _context.FollowUps.Add(followUp);
        await _context.SaveChangesAsync();

        var newDate = DateTime.Today.AddDays(7);
        var (success, _) = await _followUpService.RescheduleFollowUpAsync(
            followUp.FollowUpId, newDate, "Client requested postponement", "rep-1", RoleConstants.SalesExecutive, "Sales Rep");

        Assert.True(success);
        var updated = await _context.FollowUps.FindAsync(followUp.FollowUpId);
        Assert.Equal(newDate, updated!.FollowUpDate);
        Assert.Equal(FollowUpStatus.Planned, updated.Status);
        Assert.Contains("postponement", updated.Remarks);
    }
}
