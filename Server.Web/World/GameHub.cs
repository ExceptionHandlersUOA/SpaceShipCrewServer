﻿using Microsoft.AspNetCore.SignalR;

namespace Server.Web.World
{
    public class GameHub(Lobby gameFactory) : Hub<IGamePlayer>
    {
        public async Task<string> JoinGame()
        {
            Game game = await gameFactory.AddPlayerToGameAsync(Context);

            return game.Name;
        }
    }
}