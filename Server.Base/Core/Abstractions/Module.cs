﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Server.Base.Core.Abstractions;

public abstract class Module(ILogger logger)
{
    public readonly ILogger Logger = logger;

    public virtual string GetModuleInformation() => GetType().Namespace;

    public virtual void AddLogging(ILoggingBuilder loggingBuilder)
    {
    }

    public virtual void AddServices(IServiceCollection services, Module[] modules)
    {
    }

    public virtual void ConfigureServices(ConfigurationManager configuration, IServiceCollection services)
    {
    }

    public virtual void PostBuild(IServiceProvider services, Module[] modules)
    {
    }
}
