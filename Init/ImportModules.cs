using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Server.Base.Core.Abstractions;
using Server.Base.Logging;
using System.Linq;

namespace Init;

public static class ImportModules
{
    public static Module[] GetModules()
    {
        var modules = new[]
        {
            typeof(Server.Web.Web),
            typeof(Server.Base.Server)
        };

        var services = new ServiceCollection();

        services.AddLogging(l =>
        {
            l.AddProvider(new LoggerProvider());
            l.SetMinimumLevel(LogLevel.Trace);
        });

        foreach (var type in modules)
            services.AddSingleton(type);

        var provider = services.BuildServiceProvider();

        return modules.Select(module => provider.GetRequiredService(module) as Module).ToArray();
    }
}
