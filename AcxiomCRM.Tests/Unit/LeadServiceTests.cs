using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Authorization;
using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Models.Enums;
using AcxiomCRM.Models.ViewModels;
using AcxiomCRM.Services;
using Xunit;

namespace AcxiomCRM.Tests.Unit;

public class LeadServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _context;
    private readonly IRecordScopeService _scopeService;
    private readonly IAuditService _auditService;
    private readonly ICustomerService _customerService;
    private readonly LeadService _leadService;

    public LeadServiceTests()
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
        _customerService = new CustomerService(_context, _scopeService, _auditService);
        _leadService = new LeadService(_context, _scopeService, _auditService, _customerService);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task CreateLeadAsync_ValidLead_GeneratesLeadCodeAndPersists()
    {
        var lead = new Lead
        {
            LeadName = "Vikram Patel",
            Email = "vikram@example.com",
            Phone = "9876543210",
            CompanyName = "Patel Enterprises",
            Source = LeadSource.Website,
            Priority = LeadPriority.High,
            ExpectedValue = 250000m,
            AssignedTo = "rep-1"
        };

        var (success, message, created) = await _leadService.CreateLeadAsync(lead, "rep-1", RoleConstants.SalesExecutive, "Sales Rep");

        Assert.True(success);
        Assert.NotNull(created);
        Assert.StartsWith("LEAD-", created.LeadCode);
        Assert.Equal(LeadStatus.New, created.Status);
    }

    [Fact]
    public async Task ChangeStatusAsync_FromConverted_FailsValidation()
    {
        var lead = new Lead
        {
            LeadCode = "LEAD-0001",
            LeadName = "Converted Lead",
            Email = "conv@example.com",
            Phone = "9876543210",
            Status = LeadStatus.Converted,
            AssignedTo = "rep-1",
            CreatedBy = "system"
        };
        _context.Leads.Add(lead);
        await _context.SaveChangesAsync();

        var (success, message) = await _leadService.ChangeStatusAsync(lead.LeadId, LeadStatus.Contacted, "rep-1", RoleConstants.SalesExecutive, "Sales Rep");

        Assert.False(success);
        Assert.Contains("cannot change status", message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ConvertLeadAsync_QualifiedLead_CreatesCustomerAndOpportunity_MarksLeadConverted()
    {
        var lead = new Lead
        {
            LeadCode = "LEAD-0002",
            LeadName = "Enterprise Buyer",
            Email = "buyer@enterprise.in",
            Phone = "9876543210",
            CompanyName = "Enterprise Tech",
            Status = LeadStatus.Qualified,
            AssignedTo = "rep-1",
            CreatedBy = "system"
        };
        _context.Leads.Add(lead);
        await _context.SaveChangesAsync();

        var model = new LeadConvertViewModel
        {
            LeadId = lead.LeadId,
            CustomerName = "Enterprise Tech Ltd",
            Email = "buyer@enterprise.in",
            Phone = "9876543210",
            CompanyName = "Enterprise Tech",
            CreateOpportunity = true,
            OpportunityName = "Enterprise Cloud Rollout",
            OpportunityAmount = 750000m,
            OpportunityProbability = 40,
            ExpectedCloseDate = DateTime.Today.AddDays(45),
            AssignedTo = "rep-1"
        };

        var (success, message, customerId, opportunityId) = await _leadService.ConvertLeadAsync(
            model, "rep-1", RoleConstants.SalesExecutive, "Sales Rep");

        Assert.True(success);
        Assert.NotNull(customerId);
        Assert.NotNull(opportunityId);

        // Verify lead is marked Converted
        var updatedLead = await _context.Leads.FindAsync(lead.LeadId);
        Assert.Equal(LeadStatus.Converted, updatedLead!.Status);
        Assert.Equal(customerId, updatedLead.CustomerId);

        // Verify Customer was created
        var customer = await _context.Customers.FindAsync(customerId.Value);
        Assert.NotNull(customer);
        Assert.Equal("Enterprise Tech Ltd", customer.CustomerName);

        // Verify Opportunity was created
        var opp = await _context.Opportunities.FindAsync(opportunityId.Value);
        Assert.NotNull(opp);
        Assert.Equal(750000m, opp.Amount);
    }

    [Fact]
    public async Task ConvertLeadAsync_AlreadyConverted_PreventsDoubleConversion()
    {
        var lead = new Lead
        {
            LeadCode = "LEAD-0003",
            LeadName = "Already Converted",
            Email = "already@example.com",
            Phone = "9876543210",
            Status = LeadStatus.Converted,
            AssignedTo = "rep-1",
            CreatedBy = "system"
        };
        _context.Leads.Add(lead);
        await _context.SaveChangesAsync();

        var model = new LeadConvertViewModel
        {
            LeadId = lead.LeadId,
            CustomerName = "Second Conversion Corp",
            Email = "already@example.com",
            Phone = "9876543210",
            AssignedTo = "rep-1"
        };

        var (success, message, _, _) = await _leadService.ConvertLeadAsync(
            model, "rep-1", RoleConstants.SalesExecutive, "Sales Rep");

        Assert.False(success);
        Assert.Contains("already been converted", message, StringComparison.OrdinalIgnoreCase);
    }
}
