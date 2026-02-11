/*
    WhatExec
    Copyright (c) 2025-2026 Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using Microsoft.Extensions.DependencyInjection;
using WhatExec.Cli.Commands;
using WhatExecLib.Extensions.DependencyInjection;

if (args.Any(s => s.Contains("--interactive")))
{
    FigletText titleText = new FigletText("WhatExec").LeftJustified();

    AnsiConsole.Write(titleText);
    Console.WriteLine();
}

Cli.Ext.ConfigureServices(services =>
{
    services.AddWhatExecLib(ServiceLifetime.Singleton);
});

await Cli.RunAsync<FindCommand>(args, new CliSettings()
{
    EnablePosixBundling = true
});