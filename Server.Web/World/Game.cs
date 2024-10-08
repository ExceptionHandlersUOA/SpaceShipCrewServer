﻿using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Server.Base.Timers.Extensions;
using Server.Base.Timers.Services;
using Server.Web.Configs;
using Server.Web.Enums;
using Server.Web.Models;
using Server.Web.Protocols;
using System.Collections.Concurrent;
using System.Drawing;
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

    private Base.Timers.Timer timer;

    public bool GameReady = false;

    public GameState state = GameState.Lobby;

    public float LinearDecrease = 0;

    public StateModel StateModel => new ()
    {
         InternalRoles = IdToPlayer.Where(x => x.Key > 0).ToDictionary(x => x.Value.Role, x => x.Value),
         InternalGameState = state,
         Resources = new SendResourceModel(Resources),
         CurrentSequence = CurrentSequence
    };

    public string RoomCode { get; }

    public IGamePlayer Group { get; }

    public PlayerState Controller => IdToPlayer[0];

    private readonly Channel<int> _playerSlots;
    private readonly ConcurrentQueue<int> _availablePlayerIds = [];
    private readonly ConcurrentQueue<Role> _availableRoles = [];

    public readonly ConcurrentDictionary<string, PlayerState> ConnectionToPlayer = [];
    public readonly ConcurrentDictionary<string, int> ConnectionToId = [];

    public Dictionary<int, PlayerState> IdToPlayer => ConnectionToPlayer.Keys.ToDictionary(x => ConnectionToId[x], x => ConnectionToPlayer[x]);

    public CancellationToken Completed => _completedCts.Token;

    public string CurrentSequence = string.Empty;

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

        _lobby.WaitingGames[RoomCode] = this;

        for (var i = 0; i < config.MaxPlayersPerGame; i++)
        {
            _playerSlots.Writer.TryWrite(0);
            _availablePlayerIds.Enqueue(i + 1);
        }

        _availableRoles.Enqueue(Role.Pilot);
        _availableRoles.Enqueue(Role.Chemist);
        _availableRoles.Enqueue(Role.Engineer);

        GenerateNewSequence();
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

        await _hubContext.Clients.GroupExcept(RoomCode, connectionId).WriteMessage(new MessageModel($"The controller has joined game {RoomCode}", Color.DarkGreen));

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

            await _hubContext.Clients.GroupExcept(RoomCode, connectionId).WriteMessage(new MessageModel($"A new player joined game {RoomCode}", Color.Green));

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
                await Group.WriteMessage(new MessageModel($"Waiting for {_playerSlots.Reader.Count} player(s) to join.", Color.Yellow));

            return true;
        }

        return false;
    }

    public async Task RemovePlayerAsync(string connectionId)
    {
        if (ConnectionToPlayer.TryRemove(connectionId, out var player))
        {
            _playerSlots.Writer.TryWrite(0);
            await Group.WriteMessage(new MessageModel($"A player has left the game", Color.Red));
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

        timer = _timerThread.DelayCall((obj) => _ = DecreaseResources(obj), this, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1), 0);
    }

    public static async Task DecreaseResources(object obj)
    {
        var game = (Game) obj;

        game.Resources.Water -= .5f + game.LinearDecrease;
        game.Resources.Fuel -= .5f + game.LinearDecrease;
        game.Resources.Electricity -= .5f + game.LinearDecrease;
        game.Resources.Oxygen -= .5f + game.LinearDecrease;

        game.LinearDecrease += .05f;

        await game.CheckAndSendState();
    }

    public async Task CheckAndSendState()
    {
        if (Resources.Depleated() && state != GameState.GameOver)
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
        var deadResource = new List<string>();

        if (Resources.Oxygen < 0)
            deadResource.Add("Oxygen");

        if (Resources.Water < 0)
            deadResource.Add("Water");

        if (Resources.Fuel < 0)
            deadResource.Add("Fuel");

        if (Resources.Electricity < 0)
            deadResource.Add("Electricity");

        await Group.WriteMessage(new MessageModel($"Game has ended! It looks like the spaceship went <b>boom!</b> Due to {string.Join(", ", [.. deadResource])}", Color.LightSalmon));

        await Group.GameEnd();

        _logger.LogInformation("The game {Name} has finished.", RoomCode);

        _completedCts.Cancel();

        timer?.Stop();

        _lobby.ActiveGames.TryRemove(RoomCode, out var _);
        _lobby.WaitingGames.TryRemove(RoomCode, out var _);
    }

    public void GenerateNewSequence()
    {
        var minLength = 5;
        var maxLength = 8;

        var random = new Random();

        var length = random.Next(minLength, maxLength + 1);
        var chars = new char[length];

        var allowedChars = "OH";

        for (var i = 0; i < length; i++)
            chars[i] = allowedChars[random.Next(allowedChars.Length)];

        CurrentSequence = new string(chars);
    }
}
