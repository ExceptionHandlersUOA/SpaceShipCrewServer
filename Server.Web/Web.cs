﻿using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Server.Base.Core.Abstractions;
using Server.Web.Abstractions;
using Server.Web.Protocols;
using Server.Web.World;

namespace Server.Web;

public class Web(ILogger<Web> logger) : WebModule(logger)
{
    public override void AddServices(IServiceCollection services, Module[] modules)
    {
        services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>(); ;

        services.AddSignalR();

        services.AddSingleton<Lobby>();
        services.AddTransient<Game>();
    }

    public override void InitializeWeb(WebApplicationBuilder builder)
    {
        builder.WebHost.CaptureStartupErrors(true);

        builder.Services.AddMemoryCache();

        builder.Services.AddDataProtection().UseCryptographicAlgorithms(
            new AuthenticatedEncryptorConfiguration
            {
                EncryptionAlgorithm = EncryptionAlgorithm.AES_256_CBC,
                ValidationAlgorithm = ValidationAlgorithm.HMACSHA256
            }
        );
    }

    public override void PostWebBuild(WebApplication app)
    {
        app.UseDeveloperExceptionPage();

        app.UseHsts();

        app.UseFileServer();

        app.UseRouting();

        app.MapHub<GameHub>("/hub");

        app.UseCors(config =>
            config
                .WithOrigins("https://hackathon.feroxfoxxo.com", "https://space.feroxfoxxo.com", "http://localhost:3000")
                .AllowAnyMethod()
                .AllowCredentials()
                .AllowAnyHeader()
                .Build()
        );
    }
}
