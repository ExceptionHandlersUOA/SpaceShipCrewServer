namespace Server.Web.Protocols;
public partial class GameHub
{
    // FROM TV
    public void TutorialEnd(object _)
    {
        // Tutorial animation has concluded
        // Start game timers and sending state
        var game = GetCurrentGame();

        if (game == null)
            return;

        game.StartGame();
    }
}
