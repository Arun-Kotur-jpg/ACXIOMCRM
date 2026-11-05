using System.Threading.RateLimiting;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using AcxiomCRM.Authorization;
using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;

var builder = WebApplication.CreateBuilder(args);

// Data Protection Configuration (local keys storage)
var keysFolder = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "DataProtection-Keys");
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keysFolder))
    .SetApplicationName("AcxiomCRM");

// 1. Database Configuration (EF Core with SQLite)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=AcxiomCRM.db";
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlite(connectionString);
});

// 2. ASP.NET Core Identity Configuration
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 10;
    options.Password.RequiredUniqueChars = 1;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// 3. Cookie Configuration
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.LogoutPath = "/Account/Logout";
});

// 4. Rate Limiting for Auth
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("AuthRateLimit", opt =>
    {
        opt.PermitLimit = 10;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 0;
    });
});

// 5. Antiforgery
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
});

// 6. Authorization Policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthorizationPolicies.RequireAdmin, policy =>
        policy.RequireRole(RoleConstants.Admin));

    options.AddPolicy(AuthorizationPolicies.RequireManager, policy =>
        policy.RequireRole(RoleConstants.Admin, RoleConstants.Manager));

    options.AddPolicy(AuthorizationPolicies.RequireManagerOrAdmin, policy =>
        policy.RequireRole(RoleConstants.Admin, RoleConstants.Manager));

    options.AddPolicy(AuthorizationPolicies.RequireSalesExecutive, policy =>
        policy.RequireRole(RoleConstants.Admin, RoleConstants.Manager, RoleConstants.SalesExecutive));
});

// 7. Dependency Injection - Business Services
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IRecordScopeService, RecordScopeService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<ILeadService, LeadService>();
builder.Services.AddScoped<IOpportunityService, OpportunityService>();
builder.Services.AddScoped<IFollowUpService, FollowUpService>();
builder.Services.AddScoped<IActivityService, ActivityService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IUserService, UserService>();

// 8. MVC and API Controllers
builder.Services.AddControllersWithViews(options =>
{
    // Enable automatic antiforgery for MVC form POSTs
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute());
});

var app = builder.Build();

// 9. Database Migration and Seeding
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var config = services.GetRequiredService<IConfiguration>();

        await DbInitializer.InitializeAsync(context, userManager, roleManager, config, logger);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

// 10. HTTP Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStaticFiles();

app.UseRouting();
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

// Centralized security headers
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "SAMEORIGIN");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    await next();
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllers();

app.Run();

// Make Program public for integration testing
public partial class Program { }
