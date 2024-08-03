namespace Server.Web.Protocols;

public partial class GameHub
{
    // FROM MOBILE
    public void RoomStart()
    {
        // Moves out of the lobby, starts the tutorial part of the game
        var game = GetCurrentGame();

        if (game == null)
            return;

        game.StartTutorial();
    }
}
