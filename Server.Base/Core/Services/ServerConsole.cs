using Microsoft.Extensions.Logging;
using Server.Base.Core.Abstractions;
using Server.Base.Core.Events;
using Server.Base.Core.Models;
using Server.Base.Timers.Extensions;
using Server.Base.Timers.Services;
using Server.Base.Worlds;
using System.Globalization;
using static Server.Base.Core.Models.ConsoleCommand;

namespace Server.Base.Core.Services;

public class ServerConsole : IService
{
    private readonly Dictionary<string, ConsoleCommand> _commands;
    private readonly Thread _consoleThread;
    private readonly ServerHandler _handler;
    private readonly ILogger<ServerConsole> _logger;
    private readonly TimerThread _timerThread;
    private readonly World _world;
    private readonly EventSink _sink;

    public ServerConsole(TimerThread timerThread, ServerHandler handler, ILogger<ServerConsole> logger, World world, EventSink sink)
    {
        _timerThread = timerThread;
        _handler = handler;
        _logger = logger;
        _world = world;
        _sink = sink;

        _commands = [];

        _consoleThread = new Thread(ConsoleLoopThread)
        {
            Name = "Console Thread",
            CurrentCulture = CultureInfo.InvariantCulture
        };
    }

    public void Initialize() => _sink.ServerStarted += (_) => RunConsoleListener();

    public void RunConsoleListener()
    {
        _logger.LogDebug("Setting up console commands");

        AddCommand(
            "restart",
            "Informs players of server restart, performs a forced save, then restarts the server.",
            _ => _handler.KillServer(true)
        );

        AddCommand(
            "shutdown",
            "Performs a forced save then shuts down the app.",
            _ => _handler.KillServer(false)
        );

        AddCommand(
            "save",
            "Saves configuration files.",
            _ => _world.Save(true)
        );

        AddCommand(
            "crash",
            "Forces an exception to be thrown.",
            _ => _timerThread.DelayCall((object _) => throw new Exception("Forced Crash"), null)
        );

        DisplayHelp();

        _consoleThread.Start();
    }

    public void AddCommand(string name, string description, RunConsoleCommand commandMethod, bool strictCheck = false) =>
        _commands.Add(name, new ConsoleCommand
        {
            CommandMethod = commandMethod,
            Description = description,
            Name = name,
            StrictCheck = strictCheck
        });

    public void ConsoleLoopThread()
    {
        try
        {
            while (!_handler.IsClosing && !_handler.HasCrashed)
                ProcessCommand(Console.ReadLine());
        }
        catch (IOException)
        {
            // ignored
        }
    }

    private void ProcessCommand(string input)
    {
        if (_handler.IsClosing || _handler.HasCrashed)
            return;

        if (!string.IsNullOrEmpty(input))
        {
            var inputs = input.Trim().Split();
            var name = inputs.FirstOrDefault();

            if (name != null && _commands.TryGetValue(name, out var value))
            {
                value.CommandMethod(inputs);
                _logger.LogInformation("Successfully ran command '{Name}'", name);
            }
        }
    }

    public void DisplayHelp()
    {
        _logger.LogInformation("Commands:");

        var commands = _commands.Values
            .OrderBy(x => x.Name)
            .ToArray();

        if (commands.Length != 0)
            foreach (var command in commands)
                _logger.LogInformation("  {Name} - {Description}", command.Name, command.Description);
        else
            _logger.LogError("Could not find any commands!");
    }
}
