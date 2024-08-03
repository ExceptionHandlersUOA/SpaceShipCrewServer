using Server.Web.Enums;

namespace Server.Web.Extensions;
public static class RoleExt
{
    public static string ToString(Role role) => role switch
    {
        Role.Unknown => "unknown",
        Role.Engineer => "engineer",
        Role.Chemist => "chemist",
        Role.Pilot => "pilot",
        _ => throw new ArgumentException($"Unknown role enum: {role}"),
    };

    public static Role ToRole(string role) => role switch
    {
        "unknown" => Role.Unknown,
        "engineer" => Role.Engineer,
        "chemist" => Role.Chemist,
        "pilot" => Role.Pilot,
        _ => throw new ArgumentException($"Unknown role string: {role}"),
    };
}
