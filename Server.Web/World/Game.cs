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
    private readonly WebRConfig _config;

    private readonly CancellationTokenSource _completedCts = new();

    public ResourcesModel Resources = new ();

    public bool GameReady = false;

    public GameState state = GameState.Lobby;

    public StateModel StateModel => new ()
    {
         InternalRoles = IdToPlayer.ToDictionary(x => x.Value.Role, x => x.Value),
         InternalGameState = state,
         Resources = Resources
    };

    public string RoomCode { get; }

    private IGamePlayer Group { get; }

    public PlayerState Controller => IdToPlayer[0];

    private readonly Channel<int> _playerSlots;
    private readonly ConcurrentQueue<int> _availablePlayerIds = [];
    private readonly ConcurrentQueue<Role> _availableRoles = [];

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
        _config = config;

        RoomCode = GenerateInviteCode();
        Group = hubContext.Clients.Group(RoomCode);

        for (var i = 0; i < config.MaxPlayersPerGame; i++)
        {
            _playerSlots.Writer.TryWrite(0);
            _availablePlayerIds.Enqueue(i + 1);
        }

        _availableRoles.Enqueue(Role.Pilot);
        _availableRoles.Enqueue(Role.Chemist);
        _availableRoles.Enqueue(Role.Engineer);
    }

    public string GenerateInviteCode()
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        var random = new Random();

        var code = new string(Enumerable.Repeat(alphabet, _config.InviteLength)
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
        await CheckAndSendState();

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
                return false;

            if (_availableRoles.TryDequeue(out var role))
                playerState.Role = role;
            else
                return false;

            ConnectionToPlayer.TryAdd(connectionId, playerState);

            await _hubContext.Groups.AddToGroupAsync(connectionId, RoomCode);

            await _hubContext.Clients.GroupExcept(RoomCode, connectionId).WriteMessage($"A new player joined game {RoomCode}");
            await CheckAndSendState();

            var waitingForPlayers = true;

            if (!_playerSlots.Reader.TryPeek(out _))
            {
                _playerSlots.Writer.TryComplete();

                if (!_playerSlots.Reader.TryPeek(out _))
                {
                    waitingForPlayers = false;

                    await Group.GameReady();
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
        if (ConnectionToPlayer.TryRemove(connectionId, out var player))
        {
            _playerSlots.Writer.TryWrite(0);
            await Group.WriteMessage($"A player has left the game");
        }

        if (ConnectionToId.TryGetValue(connectionId, out var playerId))
        {
            ConnectionToId.Remove(connectionId, out var _);

            GameReady = false;

            await Group.GameNotReady();
            await CheckAndSendState();

            if (playerId > 0)
            {
                _availablePlayerIds.Enqueue(playerId);
                _availableRoles.Enqueue(player.Role);
            }
        }
    }

    public async Task StartGame()
    {
        Resources = new ResourcesModel();

        state = GameState.InGame;
        await CheckAndSendState();

        await Group.GameStart();

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

            state = GameState.GameOver;
            await CheckAndSendState();

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
