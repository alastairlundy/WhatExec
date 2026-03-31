/*
    WhatExecLite
    Copyright (c) 2025-2026 Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using WhatExec.Lib.Abstractions.Resolvers;

namespace WhatExecLite;

public class CliCommand
{
    [Command("")]
    public async Task<int> RunAsync(
        [FromServices] IPathEnvironmentVariableResolver pathEnvironmentVariableResolver,
        bool verbose = false,
        CancellationToken cancellationToken = default,
        [Argument] params string[] commands)
    {
        try
        {
            (bool success, IReadOnlyDictionary<string, FileInfo> resolvedExecutables) results = await pathEnvironmentVariableResolver.
                TryGetExecutableFilePathsAsync(commands, cancellationToken).ConfigureAwait(true);
            
            foreach (FileInfo resolvedCommand in results.resolvedExecutables.Values)
            {
                await Console.Out.WriteLineAsync(resolvedCommand.FullName).ConfigureAwait(true);
            }

            return results.success ? 0 : 1;
        }
        catch (Exception e)
        {
            await Console.Error.WriteLineAsync(Resources.Exceptions_Details).ConfigureAwait(true);
            await Console.Error.WriteLineAsync(e.Message).ConfigureAwait(true);

            if (verbose)
            {
                await Console.Error.WriteAsync(e.StackTrace).ConfigureAwait(true);
            }

            return 1;
        }
    }
}