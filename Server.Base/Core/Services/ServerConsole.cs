using Microsoft.Extensions.Logging;
using Server.Base.Core.Abstractions;
using Server.Base.Core.Events;
using Server.Base.Core.Models;
using Server.Base.Timers.Extensions;
using Server.Base.Timers.Services;
using Server.Base.Worlds;
using static Server.Base.Core.Models.ConsoleCommand;

namespace Server.Base.Core.Services;

public class ServerConsole(TimerThread timerThread, ServerHandler handler, ILogger<ServerConsole> logger, World world, EventSink sink) : IService
{
    private readonly Dictionary<string, ConsoleCommand> _commands = [];

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

        timerThread.DelayCall((_) => ProcessCommand(Console.ReadLine()), null, TimeSpan.FromSeconds(0), TimeSpan.FromMilliseconds(50), 0);
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
                ProcessCommand(Console.ReadLine());
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

            if (name != null && _commands.TryGetValue(name, out var value))
            {
                value.CommandMethod(inputs);
                logger.LogInformation("Successfully ran command '{Name}'", name);
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
