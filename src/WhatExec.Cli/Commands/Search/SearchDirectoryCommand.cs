/*
    WhatExec
    Copyright (c) 2025-2026 Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using WhatExec.Lib.Locators;

namespace WhatExec.Cli.Commands.Search;

[CliCommand(
    Name = "directory",
    Alias = "dir",
    Description = "Locate all commands and/or executable files in a given directory.",
    Parent = typeof(SearchCommand),
    TreatUnmatchedTokensAsErrors = true
)]
public class SearchDirectoryCommand
{
    private readonly IExecutablesLocator _executablesResolver;

    public SearchDirectoryCommand(IExecutablesLocator executablesResolver)
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

    [CliOption(Name = "--verbose", Description = "Enable verbose exception output.")]
    [DefaultValue(false)]
    public bool Verbose { get; set; }
    
    // The parser hands an option it does not recognise to the argument, so the argument
    // rejects a value that looks like an option and the caller is told which token failed.
    [CliArgument(Order = 0, Name = "<directory>", Description = "The directory to be searched. Required.", Required = true, ValidationPattern = "^[^-].*$", ValidationMessage = "Unrecognized option. The directory argument takes a directory path.")]
    public string? Directory { get; set; }
    
    
    public async Task<int> RunAsync(CliContext cliContext)
    {
        if (string.IsNullOrWhiteSpace(Directory))
            return ResultHelper.PrintException(new DirectoryNotFoundException(Resources.ValidationErrors_Directory_NotSpecified),
                Verbose, -1);
        
        DirectoryInfo directoryInfo = new DirectoryInfo(Directory!);

        if (!directoryInfo.Exists)
            return ResultHelper.PrintException(new DirectoryNotFoundException(Resources.ValidationErrors_Directory_NotSpecified),
                Verbose, -1);
            
        if (Limit < 1)
        { 
            Console.WriteLine(Resources.Exceptions_Commands_Find_Limit_MustBeGreaterThanZero);
            return -1;
        }

        IAsyncEnumerable<FileInfo> files = _executablesResolver.EnumerateExecutablesInDirectoryAsync(directoryInfo,
            SearchOption.AllDirectories, cliContext.CancellationToken);

        return await ResultHelper.PrintFileSearchResultsAsync(files, Limit).ConfigureAwait(true);
    }
}