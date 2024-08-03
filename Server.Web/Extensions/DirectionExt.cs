using Server.Web.Enums;

namespace Server.Web.Extensions;
public static class DirectionExt
{
    public static string ToString(Direction direction) => direction switch
    {
        Direction.Up => "up",
        Direction.Down => "down",
        _ => throw new ArgumentException($"Unknown direction enum: {direction}"),
    };

    public static Direction ToDirection(string direction) => direction switch
    {
        "up" => Direction.Up,
        "down" => Direction.Down,
        _ => throw new ArgumentException($"Unknown direction string: {direction}"),
    };
}
