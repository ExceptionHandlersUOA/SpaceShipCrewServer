using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Server.Web.Configs;
using System.Collections.Concurrent;
using System.Threading.Channels;

namespace Server.Web.World
{
    public class Game
    {
        private readonly TimeSpan _serverTimeout;

        private readonly IHubContext<GameHub, IGamePlayer> _hubContext;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger _logger;

        private readonly Lobby _lobby;

        private readonly ConcurrentDictionary<string, PlayerState> _players = new();
        private readonly CancellationTokenSource _completedCts = new();
        private readonly Channel<int> _playerSlots;

        public Game(IHubContext<GameHub, IGamePlayer> hubContext,
                    IHttpClientFactory httpClientFactory,
                    ILogger<Game> logger,
                    WebRConfig config,
                    Lobby lobby)
        {
            _hubContext = hubContext;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _playerSlots = Channel.CreateBounded<int>(config.MaxPlayersPerGame);
            _lobby = lobby;

            Name = GenerateInviteCode();
            Group = hubContext.Clients.Group(Name);

            // Give the client some buffer
            _serverTimeout = TimeSpan.FromSeconds(60);

            // Fill the slots for this game
            for (int i = 0; i < config.MaxPlayersPerGame; i++)
            {
                _playerSlots.Writer.TryWrite(0);
            }
        }

        public string Name { get; }

        private IGamePlayer Group { get; }
        public CancellationToken Completed => _completedCts.Token;

        public string GenerateInviteCode()
        {
            const string alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            var codeLength = 8;
            var code = new string(Enumerable.Repeat(alphabet, codeLength)
                .Select(s => s[random.Next(s.Length)]).ToArray());

            if (_lobby.ActiveGames.ContainsKey(code))
                return GenerateInviteCode();

            return code;
        }

        public async Task<bool> AddPlayerAsync(string connectionId)
        {
            if (_playerSlots.Reader.TryRead(out _))
            {
                _players.TryAdd(connectionId, new PlayerState
                {
                    Proxy = _hubContext.Clients.Client(connectionId)
                });

                await _hubContext.Groups.AddToGroupAsync(connectionId, Name);

                await _hubContext.Clients.GroupExcept(Name, connectionId).WriteMessage($"A new player joined game {Name}");

                var waitingForPlayers = true;

                if (!_playerSlots.Reader.TryPeek(out _))
                {
                    _playerSlots.Writer.TryComplete();

                    if (!_playerSlots.Reader.TryPeek(out _))
                    {
                        waitingForPlayers = false;
                        _ = Task.Run(PlayGame);
                    }
                }

                if (waitingForPlayers)
                    await Group.WriteMessage($"Waiting for {_playerSlots.Reader.Count} player(s) to join.");

                return true;
            }

            return false;
        }

        public async Task RemovePlayerAsync(string connectionId)
        {
            if (_players.TryRemove(connectionId, out _))
            {
                _playerSlots.Writer.TryWrite(0);
                await Group.WriteMessage($"A player has left the game");
            }
        }

        private async Task PlayGame()
        {
            var timoutTokenSource = new CancellationTokenSource();

            try
            {
                var client = _httpClientFactory.CreateClient();

                bool completed = true;

                await Group.GameStarted();

                if (completed)
                {
                    foreach (var (_, player) in _players)
                        await player.Proxy.GameCompleted();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "The game {Name} failed", Name);

                await Group.WriteMessage($"The game {Name} failed: {ex}");
            }
            finally
            {
                _logger.LogInformation("The game {Name} has finished.", Name);

                timoutTokenSource.Dispose();

                _completedCts.Cancel();
            }
        }
    }
}
