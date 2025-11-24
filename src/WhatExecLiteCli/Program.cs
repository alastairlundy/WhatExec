using AlastairLundy.WhatExecLib.Caching.Extensions;
using AlastairLundy.WhatExecLib.Extensions.DependencyInjection;
using ConsoleAppFramework;
using Microsoft.Extensions.DependencyInjection;
using WhatExecLite;

ConsoleApp.ConsoleAppBuilder app = ConsoleApp
    .Create()
    .ConfigureServices(services =>
    {
        services.AddMemoryCache();
        services.AddWhatExecLib(ServiceLifetime.Scoped);
        services.AddWhatExecLibCaching(ServiceLifetime.Scoped);
    });

app.Add<CliCommands>();

await app.RunAsync(args);
