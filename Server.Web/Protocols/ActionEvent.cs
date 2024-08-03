using Server.Web.Enums;
using Server.Web.Extensions;

namespace Server.Web.Protocols;
public partial class GameHub
{
    // FROM MOBILE
    public async Task ActionEvent(ActionEventDTO data)
    {
        // Some kind of abstract event/action happened, the ID of which is attached

        var action = ActionExt.ToAction(data.ActionId);

        var game = GetCurrentGame();

        if (game == null)
            return;

        switch(action)
        {
            case PlayerAction.CorrectFormula:
                game.Resources.Fuel += 15;
                game.Resources.Water -= 5;

                await game.CheckAndSendState();
                break;
            case PlayerAction.HarvestAsteroid:
                game.Resources.Water += 15;
                game.Resources.Electricity -= 5;

                await game.CheckAndSendState();

                break;
            case PlayerAction.MatchSine:
                game.Resources.Electricity += 15;
                game.Resources.Fuel -= 5;

                await game.CheckAndSendState();

                break;
            default:
                throw new ArgumentException($"Unknown action: {action}");
        }
    }

    public class ActionEventDTO
    {
        public string ActionId { get; set; }
    }
}
