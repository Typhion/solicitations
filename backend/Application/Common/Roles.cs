namespace Application.Common;

public static class Roles
{
    public const string Admin = "Admin";
    public const string Member = "Member";
    public static readonly IReadOnlySet<string> Assignable = new HashSet<string> { Admin, Member };
}