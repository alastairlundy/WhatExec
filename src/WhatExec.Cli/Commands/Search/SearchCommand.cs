/*
    WhatExec
    Copyright (c) 2025-2026 Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Runtime.CompilerServices;
using WhatExec.Lib.Locators;

namespace WhatExec.Cli.Commands.Search;

[CliCommand(
    Name = "search",
    Description = "Locate all commands and/or executable files on a system.",
    ShortFormAutoGenerate = CliNameAutoGenerate.None,
    Parent = typeof(FindCommand)
)]
public class SearchCommand
{
    private readonly IExecutablesLocator _executablesResolver;

    public SearchCommand(IExecutablesLocator executablesResolver)
    {
        _executablesResolver = executablesResolver;
    }

    [CliOption(
        Name = "--limit",
        Alias = "-l",
        Description = "Limits the number of results returned per command or file."
    )]
    [Range(1, int.MaxValue)]
    public int Limit { get; set; } = 1;
    
    public async Task<int> RunAsync(CliContext cliContext)
    {
        if (Limit < 1)
        { 
            Console.WriteLine(Resources.Exceptions_Commands_Find_Limit_MustBeGreaterThanZero);
            return -1;
        }
        
        IAsyncEnumerable<FileInfo> files = LocateExecutables(cliContext.CancellationToken);

        return await ResultHelper.PrintFileSearchResultsAsync(files, Limit).ConfigureAwait(true);
    }

    private async IAsyncEnumerable<FileInfo> LocateExecutables([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // The library owns the across-drives fan-out and applies the drive candidate rule, so
        // the CLI keeps no private copy of it.
        await foreach (FileInfo file in _executablesResolver
                           .EnumerateExecutablesAcrossDrivesAsync(SearchOption.AllDirectories, cancellationToken)
                           .ConfigureAwait(true))
        {
            yield return file;
        }
    }
}