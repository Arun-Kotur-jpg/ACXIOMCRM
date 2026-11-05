using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Authorization;
using AcxiomCRM.Data;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Models.Enums;
using AcxiomCRM.Services;
using Xunit;

namespace AcxiomCRM.Tests.Unit;

public class FakeAuditService : IAuditService
{
    public Task LogAsync(string? userId, string? userName, string action, string entityName, string? recordId, string? oldValue = null, string? newValue = null, string result = "Success", string? ipAddress = null, string? details = null)
    {
        return Task.CompletedTask;
    }

    public Task<(List<AuditLog> Items, int TotalCount)> GetAuditLogsAsync(string? search = null, string? module = null, string? action = null, string? result = null, DateTime? startDate = null, DateTime? endDate = null, int page = 1, int pageSize = 20)
    {
        return Task.FromResult((new List<AuditLog>(), 0));
    }
}

public class CustomerServiceTests
{
    private ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task CreateCustomerAsync_ValidCustomer_GeneratesCustomerCodeAndPersists()
    {
        using var context = CreateInMemoryDbContext();
        var scope = new RecordScopeService(context);
        var audit = new FakeAuditService();
        var service = new CustomerService(context, scope, audit);

        var customer = new Customer
        {
            CustomerName = "Apex Solutions",
            Email = "info@apex.com",
            Phone = "9876543210",
            OwnerId = "rep-1"
        };

        var (success, message, created) = await service.CreateCustomerAsync(customer, "rep-1", RoleConstants.SalesExecutive, "Sales Rep");

        Assert.True(success);
        Assert.NotNull(created);
        Assert.StartsWith("CUST-", created.CustomerCode);
        Assert.Equal(CustomerStatus.Active, created.Status);
    }

    [Fact]
    public async Task CreateCustomerAsync_DuplicateEmail_FailsWithValidationMessage()
    {
        using var context = CreateInMemoryDbContext();
        var scope = new RecordScopeService(context);
        var audit = new FakeAuditService();
        var service = new CustomerService(context, scope, audit);

        context.Customers.Add(new Customer
        {
            CustomerCode = "CUST-0001",
            CustomerName = "First Company",
            Email = "duplicate@company.com",
            Phone = "9876543210",
            OwnerId = "rep-1",
            CreatedBy = "system"
        });
        await context.SaveChangesAsync();

        var duplicateCustomer = new Customer
        {
            CustomerName = "Second Company",
            Email = "duplicate@company.com",
            Phone = "9123456789",
            OwnerId = "rep-1"
        };

        var (success, message, _) = await service.CreateCustomerAsync(duplicateCustomer, "rep-1", RoleConstants.SalesExecutive, "Sales Rep");

        Assert.False(success);
        Assert.Contains("email", message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateCustomerAsync_DuplicatePhone_FailsWithValidationMessage()
    {
        using var context = CreateInMemoryDbContext();
        var scope = new RecordScopeService(context);
        var audit = new FakeAuditService();
        var service = new CustomerService(context, scope, audit);

        context.Customers.Add(new Customer
        {
            CustomerCode = "CUST-0001",
            CustomerName = "First Company",
            Email = "first@company.com",
            Phone = "9876543210",
            OwnerId = "rep-1",
            CreatedBy = "system"
        });
        await context.SaveChangesAsync();

        var duplicateCustomer = new Customer
        {
            CustomerName = "Second Company",
            Email = "second@company.com",
            Phone = "9876543210",
            OwnerId = "rep-1"
        };

        var (success, message, _) = await service.CreateCustomerAsync(duplicateCustomer, "rep-1", RoleConstants.SalesExecutive, "Sales Rep");

        Assert.False(success);
        Assert.Contains("phone", message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeactivateCustomerAsync_ValidCustomer_SetsStatusToInactive()
    {
        using var context = CreateInMemoryDbContext();
        var scope = new RecordScopeService(context);
        var audit = new FakeAuditService();
        var service = new CustomerService(context, scope, audit);

        var customer = new Customer
        {
            CustomerId = 1,
            CustomerCode = "CUST-0001",
            CustomerName = "Active Corp",
            Email = "corp@example.com",
            Phone = "9876543210",
            Status = CustomerStatus.Active,
            OwnerId = "rep-1",
            CreatedBy = "system"
        };
        context.Customers.Add(customer);
        await context.SaveChangesAsync();

        var (success, _) = await service.DeactivateCustomerAsync(1, "rep-1", RoleConstants.SalesExecutive, "Sales Rep");

        Assert.True(success);
        var updated = await context.Customers.FindAsync(1);
        Assert.Equal(CustomerStatus.Inactive, updated!.Status);
    }
}

