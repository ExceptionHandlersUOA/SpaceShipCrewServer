using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Server.Base.Timers.Extensions;
using Server.Base.Timers.Services;
using Server.Web.Configs;
using Server.Web.Enums;
using Server.Web.Models;
using Server.Web.Protocols;
using System.Collections.Concurrent;
using System.Threading.Channels;

namespace Server.Web.World;

public class Game
{
    private readonly IHubContext<GameHub, IGamePlayer> _hubContext;
    private readonly ILogger _logger;

    private readonly Lobby _lobby;
    private readonly TimerThread _timerThread;

    private readonly CancellationTokenSource _completedCts = new();

    public ResourcesModel Resources = new ();

    public bool GameReady = false;

    public StateModel StateModel => new ()
    {
         InternalRoles = IdToPlayer.ToDictionary(x => x.Value.Role, x => x.Value),
         Resources = Resources
    };

    public string RoomCode { get; }

    private IGamePlayer Group { get; }

    public PlayerState Controller => IdToPlayer[0];

    private readonly Channel<int> _playerSlots;
    private readonly ConcurrentQueue<int> _availablePlayerIds = [];

    public readonly ConcurrentDictionary<string, PlayerState> ConnectionToPlayer = [];
    public readonly ConcurrentDictionary<string, int> ConnectionToId = [];

    public Dictionary<int, PlayerState> IdToPlayer => ConnectionToPlayer.Keys.ToDictionary(x => ConnectionToId[x], x => ConnectionToPlayer[x]);

    public CancellationToken Completed => _completedCts.Token;

    public Game(IHubContext<GameHub, IGamePlayer> hubContext,
                ILogger<Game> logger,
                WebRConfig config,
                TimerThread timerThread,
                Lobby lobby)
    {
        _hubContext = hubContext;
        _logger = logger;
        _playerSlots = Channel.CreateBounded<int>(config.MaxPlayersPerGame);
        _lobby = lobby;
        _timerThread = timerThread;

        RoomCode = GenerateInviteCode();
        Group = hubContext.Clients.Group(RoomCode);

        for (var i = 0; i < config.MaxPlayersPerGame; i++)
        {
            _playerSlots.Writer.TryWrite(0);
            _availablePlayerIds.Enqueue(i + 1);
        }
    }

    public string GenerateInviteCode()
    {
        const string alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        var random = new Random();
        var codeLength = 8;
        var code = new string(Enumerable.Repeat(alphabet, codeLength)
            .Select(s => s[random.Next(s.Length)]).ToArray());

        return _lobby.ActiveGames.ContainsKey(code) ? GenerateInviteCode() :
            _lobby.WaitingGames.ContainsKey(code) ? GenerateInviteCode() :
            code;
    }

    public async Task<bool> AddControllerAsync(string connectionId)
    {
        if (ConnectionToId.Values.Any(i => i == 0))
            return false;

        ConnectionToPlayer.TryAdd(connectionId, new PlayerState
        {
            Proxy = _hubContext.Clients.Client(connectionId),
            PlayerId = 0,
            Username = "Controller"
        });

        ConnectionToId[connectionId] = 0;

        await _hubContext.Groups.AddToGroupAsync(connectionId, RoomCode);

        await _hubContext.Clients.GroupExcept(RoomCode, connectionId).WriteMessage($"The controller has joined game {RoomCode}");

        return true;
    }

    public async Task<bool> AddPlayerAsync(string connectionId, string username)
    {
        if (_playerSlots.Reader.TryRead(out _))
        {
            var playerState = new PlayerState
            {
                Proxy = _hubContext.Clients.Client(connectionId),
                Username = username
            };

            if (_availablePlayerIds.TryDequeue(out var playerId))
            {
                ConnectionToId[connectionId] = playerId;
                playerState.PlayerId = playerId;
            }
            else
            {
                return false;
            }

            ConnectionToPlayer.TryAdd(connectionId, playerState);

            await _hubContext.Groups.AddToGroupAsync(connectionId, RoomCode);

            await _hubContext.Clients.GroupExcept(RoomCode, connectionId).WriteMessage($"A new player joined game {RoomCode}");

            var waitingForPlayers = true;

            if (!_playerSlots.Reader.TryPeek(out _))
            {
                _playerSlots.Writer.TryComplete();

                if (!_playerSlots.Reader.TryPeek(out _))
                {
                    waitingForPlayers = false;

                    GameReady = false;
                    CheckGameReady();
                }
            }

            if (waitingForPlayers)
                await Group.WriteMessage($"Waiting for {_playerSlots.Reader.Count} player(s) to join.");

            return true;
        }

        return false;
    }

    public void CheckGameReady()
    {
        if (GameReady && !ConnectionToPlayer.Values.Any(p => p.Role == Role.Unknown))
            Group.GameReady();
    }

    public async Task RemovePlayerAsync(string connectionId)
    {
        if (ConnectionToPlayer.TryRemove(connectionId, out _))
        {
            _playerSlots.Writer.TryWrite(0);
            await Group.WriteMessage($"A player has left the game");
        }

        if (ConnectionToId.TryGetValue(connectionId, out var playerId))
        {
            ConnectionToId.Remove(connectionId, out var _);

            GameReady = false;

            await Group.GameNotReady();

            if (playerId > 0)
                _availablePlayerIds.Enqueue(playerId);
        }
    }

    public void StartTutorial()
    {
        _lobby.WaitingGames.TryRemove(RoomCode, out var _);
        _lobby.ActiveGames[RoomCode] = this;

        Group.TutorialStart();
    }

    public void StartGame()
    {
        Resources = new ResourcesModel();

        _timerThread.DelayCall((obj) => _ = DecreaseResources(obj), null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1), -1);
    }

    public static async Task DecreaseResources(object obj)
    {
        var game = (Game) obj;

        game.Resources.Water--;
        game.Resources.Fuel--;
        game.Resources.Electricity--;
        game.Resources.Oxygen--;

        await game.CheckAndSendState();
    }

    public async Task CheckAndSendState()
    {
        if (Resources.Depleated())
        {
            await EndGame();
            return;
        }

        Resources.EnsureBounds();
        await Group.State(StateModel);
    }

    public async Task EndGame()
    {
        await Group.WriteMessage("Game has been completed!");

        await Group.GameEnd();

        _logger.LogInformation("The game {Name} has finished.", RoomCode);

        _completedCts.Cancel();

        _lobby.ActiveGames.TryRemove(RoomCode, out var _);
    }
}
