using TaxiDispatch.Domain;

namespace TaxiDispatch.Modules.Membership;

public static class MembershipRoleMapper
{
    public static MembershipRole FromAceRole(AceRoles role)
    {
        return role switch
        {
            AceRoles.Admin => MembershipRole.AdminUser,
            AceRoles.Driver => MembershipRole.DriverUser,
            AceRoles.Account => MembershipRole.AccountUser,
            AceRoles.User => MembershipRole.DispatchUser,
            _ => MembershipRole.DispatchUser
        };
    }

    public static MembershipRole FromLegacyIdentityRole(string? roleName)
    {
        return roleName?.Trim() switch
        {
            "Admin" => MembershipRole.AdminUser,
            "Driver" => MembershipRole.DriverUser,
            "Account" => MembershipRole.AccountUser,
            "Dispatch" => MembershipRole.DispatchUser,
            "DispatchUser" => MembershipRole.DispatchUser,
            "User" => MembershipRole.DispatchUser,
            _ => MembershipRole.DispatchUser
        };
    }

    public static string ToLegacyIdentityRole(MembershipRole role)
    {
        return role switch
        {
            MembershipRole.AdminUser => "Admin",
            MembershipRole.DriverUser => "Driver",
            MembershipRole.AccountUser => "Account",
            MembershipRole.DispatchUser => "User",
            _ => "User"
        };
    }

    public static AceRoles ToAceRole(MembershipRole role)
    {
        return role switch
        {
            MembershipRole.AdminUser => AceRoles.Admin,
            MembershipRole.DriverUser => AceRoles.Driver,
            MembershipRole.AccountUser => AceRoles.Account,
            MembershipRole.DispatchUser => AceRoles.User,
            _ => AceRoles.User
        };
    }
}
