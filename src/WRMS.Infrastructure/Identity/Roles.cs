namespace WRMS.Infrastructure.Identity;

public static class Roles
{
    public const string Admin = "Admin";
    public const string Officer = "Officer";
    public const string AuthorizedStaff = "AuthorizedStaff";

    public static readonly string[] All = { Admin, Officer, AuthorizedStaff };
}
