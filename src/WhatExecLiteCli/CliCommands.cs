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
            bool success = pathExecutableResolver.TryResolveAllExecutables(commands,
                out IReadOnlyDictionary<string, FileInfo> resolvedExecutables);
            

            foreach (FileInfo resolvedCommand in resolvedExecutables.Values)
            {
                Console.Out.WriteLine(resolvedCommand.FullName);
            }

            return success ? 0 : 1;
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(
                "We ran into a problem. Here are the Exception details in case you need it: "
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
        bool foundAny = pathExecutableResolver.TryResolveExecutables(
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
