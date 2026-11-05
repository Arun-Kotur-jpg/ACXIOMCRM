using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.Authorization;

namespace AcxiomCRM.Controllers;

public abstract class BaseController : Controller
{
    protected string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
    protected string CurrentUserName => User.Identity?.Name ?? "System";
    protected string CurrentUserRole
    {
        get
        {
            if (User.IsInRole(RoleConstants.Admin)) return RoleConstants.Admin;
            if (User.IsInRole(RoleConstants.Manager)) return RoleConstants.Manager;
            return RoleConstants.SalesExecutive;
        }
    }
}

