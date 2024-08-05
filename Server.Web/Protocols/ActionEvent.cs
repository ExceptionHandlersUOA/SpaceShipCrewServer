using Server.Web.Enums;
using Server.Web.Extensions;
using Server.Web.Models;
using System.Drawing;

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

        var player = game.ConnectionToPlayer[Context.ConnectionId];

        switch(action)
        {
            case PlayerAction.CorrectFormula:
                float fuelChange = game.CurrentSequence.Length * 3;

                game.Resources.Fuel += fuelChange;
                game.Resources.Water -= fuelChange / 3;

                game.GenerateNewSequence();

                await game.CheckAndSendState();

                await game.Group.WriteMessage(new MessageModel($"Vrrm, get ready for hyperspeed! {player.Username} has created <b>fuel</b>.", Color.Gold));

                break;
            case PlayerAction.HarvestAsteroid:
                const float asteroidChange = 10;

                game.Resources.Water += asteroidChange;
                game.Resources.Electricity -= asteroidChange / 3;

                await game.CheckAndSendState();

                await game.Group.WriteMessage(new MessageModel($"<b>Water</b> water everywhere - an asteroid has been harvested by {player.Username}!", Color.Gold));

                break;
            case PlayerAction.MatchSine:
                const float sineChange = 20;

                game.Resources.Electricity += sineChange;
                game.Resources.Fuel -= sineChange / 3;

                await game.Group.WriteMessage(new MessageModel($"The light of my life - {player.Username} - has generated <b>electricity</b>!", Color.Gold));

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
