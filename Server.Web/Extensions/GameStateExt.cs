using Server.Web.Enums;

namespace Server.Web.Extensions;
public static class GameStateExt
{
    public static string ToString(GameState state) => state switch
    {
        GameState.Lobby => "lobby",
        GameState.InTutorial => "inTutorial",
        GameState.InGame => "inGame",
        GameState.GameOver => "gameOver",
        _ => throw new ArgumentException($"Unknown game state enum: {state}"),
    };

    public static GameState ToGameState(string state) => state switch
    {
        "lobby" => GameState.Lobby,
        "inTutorial" => GameState.InTutorial,
        "inGame" => GameState.InGame,
        "gameOver" => GameState.GameOver,
        _ => throw new ArgumentException($"Unknown game state string: {state}"),
    };
}
