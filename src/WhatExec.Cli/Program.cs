/*
    WhatExecLib
    Copyright (c) 2025 Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Globalization;
using AlastairLundy.WhatExecLib.Caching.Extensions;
using AlastairLundy.WhatExecLib.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli.Extensions.DependencyInjection;

IServiceCollection services = new ServiceCollection();

services.AddMemoryCache();
services.AddWhatExecLib(ServiceLifetime.Scoped);
services.AddWhatExecLibCaching(ServiceLifetime.Scoped);

if (args.Any(s => s.Contains("--interactive")))
{
    FigletText titleText = new FigletText("WhatExec").Centered();

    AnsiConsole.Write(titleText);
    Console.WriteLine();
}

using DependencyInjectionRegistrar registrar = new DependencyInjectionRegistrar(services);
CommandApp app = new CommandApp(registrar);

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

            /*conf.AddCommand<DirectoryOnlySearchCommand>("directory").WithAlias("dir");

            conf.AddCommand<DriveOnlySearchCommand>("drive");

            conf.AddCommand<GlobalSearchCommand>("system");*/
        }
    );
});

app.SetDefaultCommand<PathOnlySearchCommand>();

return app.Run(args);
