namespace AcxiomCRM.Authorization;

public static class AuthorizationPolicies
{
    public const string RequireAdmin = "RequireAdmin";
    public const string RequireManager = "RequireManager";
    public const string RequireSalesExecutive = "RequireSalesExecutive";
    public const string RequireManagerOrAdmin = "RequireManagerOrAdmin";
}

