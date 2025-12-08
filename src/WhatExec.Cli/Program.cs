/*
    WhatExecLib
    Copyright (c) 2025 Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using DotMake.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using WhatExec.Cli.Commands;
using WhatExecLib.Extensions.DependencyInjection;

if (args.Any(s => s.Contains("--interactive")))
{
    FigletText titleText = new FigletText("WhatExec").Centered();

    AnsiConsole.Write(titleText);
    Console.WriteLine();
}

Cli.Ext.ConfigureServices(services =>
{
    services.AddMemoryCache();
    services.AddWhatExecLib(ServiceLifetime.Scoped);
    services.AddWhatExecLibCaching(ServiceLifetime.Scoped);
});

Cli.Run<RootCliCommand>();
