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
    private readonly Dictionary<string, ConsoleCommand> _commands = [];
    private readonly TimerThread timerThread;
    private readonly ServerHandler handler;
    private readonly ILogger<ServerConsole> logger;
    private readonly World world;
    private readonly EventSink sink;
    private readonly Thread _consoleThread;

    public ServerConsole(TimerThread timerThread, ServerHandler handler, ILogger<ServerConsole> logger, World world, EventSink sink)
    {
        this.timerThread = timerThread;
        this.handler = handler;
        this.logger = logger;
        this.world = world;
        this.sink = sink;

        _consoleThread = new Thread(ConsoleLoopThread)
        {
            Name = "Console Thread",
            CurrentCulture = CultureInfo.InvariantCulture
        };
    }

    public void Initialize() => sink.ServerStarted += (_) => RunConsoleListener();

    public void RunConsoleListener()
    {
        logger.LogDebug("Setting up console commands");

        AddCommand(
            "shutdown",
            "Performs a forced save then shuts down the app.",
            _ => handler.KillServer()
        );

        AddCommand(
            "save",
            "Saves configuration files.",
            _ => world.Save(true)
        );

        AddCommand(
            "crash",
            "Forces an exception to be thrown.",
            _ => timerThread.DelayCall((object _) => throw new Exception("Forced Crash"), null)
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
            while (!handler.IsClosing && !handler.HasCrashed)
            {
                var input = Console.ReadLine();

                if (input != null)
                {
                    ProcessCommand(input);
                }

                Thread.Sleep(100);
            }
        }
        catch (IOException)
        {
            // ignored
        }
    }

    private void ProcessCommand(string input)
    {
        if (handler.IsClosing || handler.HasCrashed)
            return;

        if (!string.IsNullOrEmpty(input))
        {
            var inputs = input.Trim().Split();
            var name = inputs.FirstOrDefault();

            if (name != null)
            {
                if (_commands.TryGetValue(name, out var value))
                {
                    value.CommandMethod(inputs);
                    logger.LogInformation("Successfully ran command '{Name}'", name);
                }
                else
                    DisplayHelp();
            }
        }
    }

    public void DisplayHelp()
    {
        logger.LogInformation("Commands:");

        var commands = _commands.Values
            .OrderBy(x => x.Name)
            .ToArray();

        if (commands.Length != 0)
            foreach (var command in commands)
                logger.LogInformation("  {Name} - {Description}", command.Name, command.Description);
        else
            logger.LogError("Could not find any commands!");
    }
}
