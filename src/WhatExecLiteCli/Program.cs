/*
    WhatExecLite
    Copyright (c) 2025 Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using ConsoleAppFramework;

using Microsoft.Extensions.DependencyInjection;
using WhatExecLib.Extensions.DependencyInjection;
using WhatExecLite;

ConsoleApp.ConsoleAppBuilder app = ConsoleApp
    .Create()
    .ConfigureServices(services =>
    {
        services.AddWhatExecLib(ServiceLifetime.Scoped);
    });

app.Add<CliCommands>();

await app.RunAsync(args);
