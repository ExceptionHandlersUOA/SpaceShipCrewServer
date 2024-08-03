using Server.Web.Enums;

namespace Server.Web.Extensions;
public static class ActionExt
{
    public static string ToString(PlayerAction action) => action switch
    {
        PlayerAction.HarvestAsteroid => "harvestAsteroid",
        PlayerAction.MatchSine => "matchSine",
        PlayerAction.CorrectFormula => "correctFormula",
        _ => throw new ArgumentException($"Unknown action enum: {action}"),
    };

    public static PlayerAction ToAction(string action) => action switch
    {
        "harvestAsteroid" => PlayerAction.HarvestAsteroid,
        "matchSine" => PlayerAction.MatchSine,
        "correctFormula" => PlayerAction.CorrectFormula,
        _ => throw new ArgumentException($"Unknown action string: {action}"),
    };
}
