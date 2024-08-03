using Server.Base.Core.Abstractions;
using Server.Base.Core.Extensions;

namespace Server.Base.Core.Configs;

public class InternalRConfig : IRConfig
{
    public string CrashDirectory { get; set; }
    public string LogDirectory { get; set; }

    public int BreakCount { get; }
    public double[] Delays { get; }

    public string ServerShutdownMessage { get; }

    public double DisconnectionTimeout { get; }

    public bool RestartOnCrash { get; }

    public InternalRConfig()
    {
        CrashDirectory = InternalDirectory.GetDirectory("Crashed");
        LogDirectory = InternalDirectory.GetDirectory("Logs");

        BreakCount = 20000;
        Delays = [0, 10, 25, 50, 250, 1000, 5000, 60000];

        DisconnectionTimeout = 100000;

        ServerShutdownMessage = "Server is shutting down!";

        RestartOnCrash = false;
    }
}
