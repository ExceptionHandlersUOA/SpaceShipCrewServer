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
                game.Resources.Fuel += game.CurrentSequence.Length * 3;
                game.Resources.Water -= game.CurrentSequence.Length;

                game.GenerateNewSequence();

                await game.CheckAndSendState();

                await game.Group.WriteMessage(new MessageModel($"Vrrm, get ready for hyperspeed! {player.Username} has created <b>fuel</b>.", Color.Gold));

                break;
            case PlayerAction.HarvestAsteroid:
                game.Resources.Water += 10;
                game.Resources.Electricity -= 5;

                await game.CheckAndSendState();

                await game.Group.WriteMessage(new MessageModel($"<b>Water</b> water everywhere - an asteroid has been harvested by {player.Username}!", Color.Gold));

                break;
            case PlayerAction.MatchSine:
                game.Resources.Electricity += 20;
                game.Resources.Fuel -= 5;

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
