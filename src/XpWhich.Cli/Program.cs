using System;
using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;
using Spectre.Console.Cli.Extensions.DependencyInjection;
using XpWhich.Cli.Commands;

using XpWhichLib;
using XpWhichLib.Abstractions;
using XpWhichLib.Detectors;

IServiceCollection services = new ServiceCollection();

services.AddScoped<IExecutableFileDetector, ExecutableFileDetector>();
services.AddScoped<IExecutableFileInstancesLocator, IExecutableFileInstancesLocator>();
services.AddScoped<IMultiExecutableLocator, MultiExecutableLocator>();

using var registrar = new DependencyInjectionRegistrar(services);
CommandApp app = new CommandApp(registrar);

app.Configure(config =>
{
    config.CaseSensitivity(CaseSensitivity.Commands);
    config.SetApplicationCulture(CultureInfo.CurrentCulture);
    config.SetApplicationName("xpwhich");
    config.UseAssemblyInformationalVersion();

    Array.ForEach(args, x =>
    {
        if (x.ToLower().Contains("pretty"))
            app.SetDefaultCommand<PrettyXpWhichCommand>();
        else
        {
            app.SetDefaultCommand<XpWhichCommand>();
        }
    });

    config.AddCommand<PrettyXpWhichCommand>("pretty");
    
    config.AddCommand<WhichCompatCommand>("posix")
        .WithAlias("nix");
});


return app.Run(args);