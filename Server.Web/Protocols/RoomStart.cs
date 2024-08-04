namespace Server.Web.Protocols;

public partial class GameHub
{
    // FROM MOBILE
    public async Task RoomStart(object _)
    {
        // Moves out of the lobby, starts the game
        var game = GetCurrentGame();

        if (game == null)
            return;

        await game.StartGame();
    }
}
