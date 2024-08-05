using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Server.Base.Core.Configs;
using Server.Base.Core.Events;
using Server.Base.Core.Events.Arguments;
using Server.Base.Core.Extensions;
using Server.Base.Core.Services;
using Server.Base.Logging.Internal;
using Server.Base.Timers.Services;
using Server.Base.Worlds;
using System.Globalization;
using System.Reflection;
using System.Runtime;
using System.Runtime.Versioning;

namespace Server.Base.Core.Workers;

public class ServerWorker : IHostedService
{
    private readonly ILogger<ServerWorker> _logger;
    private readonly ServerHandler _serverHandler;
    private readonly EventSink _sink;
    private readonly TimerThread _timerThread;
    private readonly World _world;
    private readonly InternalRConfig _config;

    public readonly MultiTextWriter MultiConsoleOut;

    public ServerWorker(ILogger<ServerWorker> logger,
        ServerHandler serverHandler, TimerThread timerThread, World world,
        EventSink sink, InternalRConfig config)
    {
        _logger = logger;
        _serverHandler = serverHandler;
        _timerThread = timerThread;
        _world = world;
        _sink = sink;
        _config = config;

        var fileHandler = new ConsoleFileLogger("console.log", config);
        MultiConsoleOut = new MultiTextWriter(Console.Out, fileHandler);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _sink.InternalShutdown += OnClose;

        Thread.CurrentThread.Name = "Main Thread";
        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

        var baseDirectory = InternalDirectory.GetBaseDirectory();

        if (baseDirectory.Length > 0)
            Directory.SetCurrentDirectory(baseDirectory);

        try
        {
            Console.SetOut(MultiConsoleOut);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not get log directory!");
        }

        foreach (var module in _serverHandler.Modules)
            _logger.LogDebug("{ModuleInfo}", module.GetModuleInformation());

        if (GetTicks.IsLinux)
            _logger.LogWarning("Unix environment detected");

        var frameworkName = Assembly.GetEntryAssembly()?
            .GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName;

        _logger.LogDebug("Compiled for {OS} and running on {NetVersion}", GetTicks.IsLinux ? "UNIX " : "WINDOWS",
            string.IsNullOrEmpty(frameworkName) ? "UNKNOWN" : frameworkName);

        if (GCSettings.IsServerGC)
            _logger.LogDebug("Server garbage collection mode enabled");

        _world.Load();
        _sink.InvokeServerStarted(new ServerStartedEventArgs(_serverHandler.Modules));

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _serverHandler.HandleClosed();
        return Task.CompletedTask;
    }

    public void OnClose()
    {
        _world.Save(false);
        _world.Broadcast(_config.ServerShutdownMessage);
        
        _timerThread.Set();
    }
}
