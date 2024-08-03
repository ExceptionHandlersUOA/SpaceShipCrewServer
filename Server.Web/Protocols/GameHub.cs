using Microsoft.AspNetCore.SignalR;
using Server.Web.World;

namespace Server.Web.Protocols;

public partial class GameHub(Lobby lobby) : Hub<IGamePlayer>
{
    public Game GetCurrentGame() =>
        Context.Items.TryGetValue("Game", out var game) ? (Game)game : null;
}
