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

public class OpportunityServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _context;
    private readonly IRecordScopeService _scopeService;
    private readonly IAuditService _auditService;
    private readonly OpportunityService _opportunityService;

    public OpportunityServiceTests()
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
        _opportunityService = new OpportunityService(_context, _scopeService, _auditService);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    private Customer SeedCustomer(string ownerId = "rep-1")
    {
        var customer = new Customer
        {
            CustomerCode = "CUST-0001",
            CustomerName = "Test Client Ltd",
            Email = "client@test.com",
            Phone = "9876543210",
            OwnerId = ownerId,
            CreatedBy = "system"
        };
        _context.Customers.Add(customer);
        _context.SaveChanges();
        return customer;
    }

    [Fact]
    public async Task CreateOpportunityAsync_AmountZeroOrNegative_FailsValidation()
    {
        var customer = SeedCustomer();

        var opp = new Opportunity
        {
            OpportunityName = "Zero Amount Deal",
            CustomerId = customer.CustomerId,
            Amount = 0m, // Invalid
            Stage = OpportunityStage.Qualification,
            Probability = 20,
            ExpectedCloseDate = DateTime.Today.AddDays(30),
            AssignedTo = "rep-1"
        };

        var (success, message, _) = await _opportunityService.CreateOpportunityAsync(
            opp, "rep-1", RoleConstants.SalesExecutive, "Sales Rep");

        Assert.False(success);
        Assert.Contains("greater than 0", message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(-5)]
    [InlineData(105)]
    public async Task CreateOpportunityAsync_InvalidProbability_FailsValidation(int prob)
    {
        var customer = SeedCustomer();

        var opp = new Opportunity
        {
            OpportunityName = "Invalid Prob Deal",
            CustomerId = customer.CustomerId,
            Amount = 50000m,
            Stage = OpportunityStage.Qualification,
            Probability = prob, // Invalid
            ExpectedCloseDate = DateTime.Today.AddDays(30),
            AssignedTo = "rep-1"
        };

        var (success, message, _) = await _opportunityService.CreateOpportunityAsync(
            opp, "rep-1", RoleConstants.SalesExecutive, "Sales Rep");

        Assert.False(success);
        Assert.Contains("between 0 and 100", message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateOpportunityAsync_ValidOpportunity_CalculatesWeightedPipelineAndSaves()
    {
        var customer = SeedCustomer();

        var opp = new Opportunity
        {
            OpportunityName = "ERP Integration",
            CustomerId = customer.CustomerId,
            Amount = 100000m,
            Stage = OpportunityStage.Proposal,
            Probability = 50,
            ExpectedCloseDate = DateTime.Today.AddDays(30),
            AssignedTo = "rep-1"
        };

        var (success, message, created) = await _opportunityService.CreateOpportunityAsync(
            opp, "rep-1", RoleConstants.SalesExecutive, "Sales Rep");

        Assert.True(success);
        Assert.NotNull(created);
        Assert.Equal(50000m, created.WeightedAmount); // 100,000 * 50% = 50,000
    }

    [Fact]
    public async Task ChangeStageAsync_MoveToWon_SynchronizesStatusAndSetsProbabilityTo100()
    {
        var customer = SeedCustomer();

        var opp = new Opportunity
        {
            OpportunityName = "Closing Deal",
            CustomerId = customer.CustomerId,
            Amount = 200000m,
            Stage = OpportunityStage.Negotiation,
            Probability = 75,
            Status = OpportunityStatus.Open,
            ExpectedCloseDate = DateTime.Today.AddDays(30),
            AssignedTo = "rep-1",
            CreatedBy = "system"
        };
        _context.Opportunities.Add(opp);
        await _context.SaveChangesAsync();

        var (success, _) = await _opportunityService.ChangeStageAsync(
            opp.OpportunityId, OpportunityStage.Won, "rep-1", RoleConstants.SalesExecutive, "Sales Rep");

        Assert.True(success);
        var updated = await _context.Opportunities.FindAsync(opp.OpportunityId);
        Assert.Equal(OpportunityStage.Won, updated!.Stage);
        Assert.Equal(OpportunityStatus.Won, updated.Status);
        Assert.Equal(100, updated.Probability);
    }
}
