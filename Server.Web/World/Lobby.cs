using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Server.Base.Core.Abstractions;
using System.Collections.Concurrent;

namespace Server.Web.World
{
    public class Lobby(IServiceProvider serviceProvider)
    {
        private readonly ConcurrentQueue<Game> _waitingGames = new();

        public readonly ConcurrentDictionary<string, Game> ActiveGames = new();

        private static readonly object _gameKey = new();

        public async Task<Game> AddPlayerToGameAsync(HubCallerContext hubCallerContext)
        {
            if (hubCallerContext.Items[_gameKey] is Game g)
                return g;

            while (true)
            {
                if (_waitingGames.TryPeek(out var game))
                {
                    if (!await game.AddPlayerAsync(hubCallerContext.ConnectionId))
                    {
                        if (ActiveGames.TryAdd(game.Name, game))
                        {
                            game.Completed.UnsafeRegister(_ =>
                            {
                                ActiveGames.TryRemove(game.Name, out var _);
                            },
                            null);

                            _waitingGames.TryDequeue(out _);
                        }

                        continue;
                    }
                    else
                    {
                        hubCallerContext.Items[_gameKey] = game;

                        hubCallerContext.ConnectionAborted.Register(() =>
                        {
                            _ = game.RemovePlayerAsync(hubCallerContext.ConnectionId);
                        });

                        game.Completed.Register(() => hubCallerContext.Items.Remove(_gameKey));
                    }

                    return game;
                }

                var newGame = serviceProvider.GetRequiredService<Game>();

                _waitingGames.Enqueue(newGame);
            }
        }
    }
}
