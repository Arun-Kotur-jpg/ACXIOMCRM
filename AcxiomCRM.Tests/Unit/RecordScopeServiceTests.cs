using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Authorization;
using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Services;
using Xunit;

namespace AcxiomCRM.Tests.Unit;

public class RecordScopeServiceTests
{
    private ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task GetAllowedUserIdsAsync_AdminRole_ReturnsNull()
    {
        using var context = CreateInMemoryDbContext();
        var service = new RecordScopeService(context);

        var result = await service.GetAllowedUserIdsAsync("admin-id", RoleConstants.Admin);

        Assert.Null(result); // null represents unrestricted system-wide access
    }

    [Fact]
    public async Task GetAllowedUserIdsAsync_SalesExecutiveRole_ReturnsOnlySelf()
    {
        using var context = CreateInMemoryDbContext();
        var service = new RecordScopeService(context);

        var result = await service.GetAllowedUserIdsAsync("sales-1", RoleConstants.SalesExecutive);

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Contains("sales-1", result);
    }

    [Fact]
    public async Task GetAllowedUserIdsAsync_ManagerRole_ReturnsSelfAndActiveSubordinates()
    {
        using var context = CreateInMemoryDbContext();

        var managerId = "mgr-1";
        context.Users.AddRange(
            new ApplicationUser { Id = managerId, UserName = "mgr", FullName = "Manager 1", IsActive = true },
            new ApplicationUser { Id = "sub-1", UserName = "sub1", FullName = "Sub 1", ManagerId = managerId, IsActive = true },
            new ApplicationUser { Id = "sub-2", UserName = "sub2", FullName = "Sub 2", ManagerId = managerId, IsActive = true },
            new ApplicationUser { Id = "sub-inactive", UserName = "subInact", FullName = "Sub Inactive", ManagerId = managerId, IsActive = false },
            new ApplicationUser { Id = "other-rep", UserName = "other", FullName = "Other Rep", ManagerId = "mgr-2", IsActive = true }
        );
        await context.SaveChangesAsync();

        var service = new RecordScopeService(context);
        var result = await service.GetAllowedUserIdsAsync(managerId, RoleConstants.Manager);

        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Contains(managerId, result);
        Assert.Contains("sub-1", result);
        Assert.Contains("sub-2", result);
        Assert.DoesNotContain("sub-inactive", result);
        Assert.DoesNotContain("other-rep", result);
    }

    [Fact]
    public async Task CanAccessCustomerAsync_SalesExecutive_DeniesAccessToOthersRecord_PreventsIDOR()
    {
        using var context = CreateInMemoryDbContext();
        context.Customers.Add(new Customer
        {
            CustomerId = 1,
            CustomerCode = "CUST-0001",
            CustomerName = "Target Corp",
            Email = "target@example.com",
            Phone = "9876543210",
            OwnerId = "sales-2",
            CreatedBy = "system"
        });
        await context.SaveChangesAsync();

        var service = new RecordScopeService(context);

        // Sales executive 1 attempts to access sales executive 2's customer
        var canAccess = await service.CanAccessCustomerAsync(1, "sales-1", RoleConstants.SalesExecutive);

        Assert.False(canAccess); // Must prevent Insecure Direct Object Reference (IDOR)
    }

    [Fact]
    public async Task CanAccessCustomerAsync_Admin_CanAccessAnyRecord()
    {
        using var context = CreateInMemoryDbContext();
        context.Customers.Add(new Customer
        {
            CustomerId = 1,
            CustomerCode = "CUST-0001",
            CustomerName = "Target Corp",
            Email = "target@example.com",
            Phone = "9876543210",
            OwnerId = "sales-2",
            CreatedBy = "system"
        });
        await context.SaveChangesAsync();

        var service = new RecordScopeService(context);
        var canAccess = await service.CanAccessCustomerAsync(1, "admin-user", RoleConstants.Admin);

        Assert.True(canAccess);
    }
}

