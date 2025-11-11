using System;
using System.Globalization;
using AlastairLundy.WhatExec.Cli.Commands;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Cli.Extensions.DependencyInjection;
using WhatExecLib.Extensions.DependencyInjection.DependencyInjection;

IServiceCollection services = new ServiceCollection();

services.AddWhatExecLib(ServiceLifetime.Scoped);

using DependencyInjectionRegistrar registrar = new DependencyInjectionRegistrar(services);
CommandApp app = new CommandApp(registrar);

FigletText titleText = new FigletText("WhatExec").Centered();

AnsiConsole.Write(titleText);
Console.WriteLine();

app.Configure(config =>
{
    config.CaseSensitivity(CaseSensitivity.Commands);
    config.SetApplicationCulture(CultureInfo.CurrentCulture);
    config.SetApplicationName("whatexec");
    config.UseAssemblyInformationalVersion();

    config.AddBranch(
        "search",
        conf =>
        {
            conf.AddCommand<PathOnlySearchCommand>("path");

            conf.AddCommand<DirectoryOnlySearchCommand>("directory").WithAlias("dir");

            conf.AddCommand<DriveOnlySearchCommand>("drive");

            conf.AddCommand<GlobalSearchCommand>("system");
        }
    );
});

return app.Run(args);
