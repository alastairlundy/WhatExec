/*
    WhatExecLite
    Copyright (c) 2025 Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using ConsoleAppFramework;
using WhatExecLib.Abstractions;

namespace WhatExecLite;

public class CliCommands
{
    [Command("")]
    public int Run(
        [FromServices] IPathExecutableResolver pathExecutableResolver,
        bool verbose = false,
        [Argument] params string[] commands
    )
    {
        try
        {
            IEnumerable<FileInfo> resolvedCommands = ResolveCommands(
                pathExecutableResolver,
                commands
            );

            foreach (FileInfo resolvedCommand in resolvedCommands)
            {
                Console.Out.WriteLine(resolvedCommand.FullName);
            }

            return 0;
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(
                "We ran into a problem. Here's the Exception details in case you need it: "
            );
            Console.Error.WriteLine(e.Message);

            if (verbose)
            {
                Console.Error.Write(e.StackTrace);
            }

            return 1;
        }
    }

    private IEnumerable<FileInfo> ResolveCommands(
        IPathExecutableResolver pathExecutableResolver,
        string[] commands
    )
    {
        bool foundAny = pathExecutableResolver.TryResolveExecutableFiles(
            commands,
            out FileInfo[]? files
        );

        if (foundAny && files is not null)
        {
            return files;
        }
        return [];
    }
}
