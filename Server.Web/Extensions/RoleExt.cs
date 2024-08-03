using Server.Web.Enums;

namespace Server.Web.Extensions;
public static class RoleExt
{
    public static string ToString(Role role) => role switch
    {
        Role.Engineer => "engineer",
        Role.Chemist => "chemist",
        Role.Pilot => "pilot",
        _ => throw new ArgumentException($"Unknown role enum: {role}"),
    };

    public static Role ToRole(string role) => role switch
    {
        "engineer" => Role.Engineer,
        "chemist" => Role.Chemist,
        "pilot" => Role.Pilot,
        _ => throw new ArgumentException($"Unknown role string: {role}"),
    };
}
