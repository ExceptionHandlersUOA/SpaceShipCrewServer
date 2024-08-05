using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Server.Base.Core.Abstractions;
using Server.Base.Core.Configs;
using Server.Base.Core.Events;
using Server.Base.Core.Events.Arguments;
using Server.Base.Core.Extensions;
using Server.Base.Worlds;

namespace Server.Base.Core.Services;

public class CrashGuard(ILogger<CrashGuard> logger, EventSink sink,
    IServiceProvider services, InternalRConfig config, World world) : IService
{
    private readonly Module[] _modules = services.GetServices<Module>().ToArray();

    public void Initialize() => sink.Crashed += OnCrash;

    public void OnCrash(CrashedEventArgs e)
    {
        GenerateCrashReport(e);

        world.Save(false);
    }

    private void GenerateCrashReport(CrashedEventArgs crashedEventArgs)
    {
        logger.LogDebug("Generating report...");

        try
        {
            var timeStamp = GetTime.GetTimeStamp();
            var fileName = $"Crash {timeStamp}.log";

            var filePath = Path.Combine(config.CrashDirectory, fileName);

            using (var streamWriter = new StreamWriter(filePath))
            {
                streamWriter.WriteLine("Server Crash Report");
                streamWriter.WriteLine("===================");
                streamWriter.WriteLine();

                foreach (var module in _modules)
                    streamWriter.WriteLine(module.GetModuleInformation());
                streamWriter.WriteLine("Operating System: {0}", Environment.OSVersion);
                streamWriter.WriteLine(".NET: {0}", Environment.Version);
                streamWriter.WriteLine("Time: {0}", DateTime.UtcNow);

                streamWriter.WriteLine();
                streamWriter.WriteLine("Exception:");
                streamWriter.WriteLine(crashedEventArgs.Exception);
                streamWriter.WriteLine();
            }

            logger.LogInformation("Logged error!");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unable to log error.");
        }
    }
}
