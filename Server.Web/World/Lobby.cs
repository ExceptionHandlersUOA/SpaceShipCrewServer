using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;

namespace Server.Web.World;

public class Lobby(IServiceProvider serviceProvider)
{
    public readonly ConcurrentDictionary<string, Game> WaitingGames = [];
    public readonly ConcurrentDictionary<string, Game> ActiveGames = [];

    public async Task<Game> AddControllerToGameAsync(HubCallerContext hubCallerContext)
    {
        var newGame = serviceProvider.GetRequiredService<Game>();

        if (!await newGame.AddControllerAsync(hubCallerContext.ConnectionId))
            return null;

        LinkGameToPlayer(hubCallerContext, newGame);

        return newGame;
    }

    public async Task<int> AddPlayerToGameAsync(HubCallerContext hubCallerContext, string inviteCode, string username)
    {
        if (!WaitingGames.TryGetValue(inviteCode, out var game))
            return -1;

        if (!await game.AddPlayerAsync(hubCallerContext.ConnectionId, username))
            return -1;

        LinkGameToPlayer(hubCallerContext, game);

        return game.ConnectionToId[hubCallerContext.ConnectionId];
    }

    private static void LinkGameToPlayer(HubCallerContext hubCallerContext, Game game)
    {
        hubCallerContext.Items["Game"] = game;

        hubCallerContext.ConnectionAborted.Register(() => _ = game.RemovePlayerAsync(hubCallerContext.ConnectionId));

        game.Completed.Register(() => hubCallerContext.Items.Remove("Game"));
    }
}
