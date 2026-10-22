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
    Name = "drive",
    Description = "Locate all commands and/or executable files on a Storage Drive.",
    Parent = typeof(SearchCommand),
    TreatUnmatchedTokensAsErrors = true
)]
public class SearchDriveCommand
{
    private readonly IExecutablesLocator _executablesResolver;

    public SearchDriveCommand(IExecutablesLocator executablesResolver)
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

    // The parser hands an option it does not recognise to the argument, so the argument
    // rejects a value that looks like an option and the caller is told which token failed.
    [CliArgument(Order = 0, Name = "<drive>", Description = "The drive to search. Required.", Required = true, ValidationPattern = "^[^-].*$", ValidationMessage = "Unrecognized option. The drive argument takes a drive name such as C or C:\\.")]
    public string? Drive { get; set; }

    [CliOption(Name = "--verbose", Description = "Enable verbose exception output.")]
    [DefaultValue(false)]
    public bool Verbose { get; set; }
    
    public async Task<int> RunAsync(CliContext cliContext)
    {
        DriveInfo drive;

        try
        {
            drive = new DriveInfo(Drive!);
        }
        catch (ArgumentException)
        {
            return ResultHelper.PrintException(new DriveNotFoundException(Resources.ValidationErrors_Drive_NotSpecified),
                Verbose, -1);
        }

        if (Limit < 1)
        { 
            Console.WriteLine(Resources.Exceptions_Commands_Find_Limit_MustBeGreaterThanZero);
            return -1;
        }

        IAsyncEnumerable<FileInfo> files = _executablesResolver.EnumerateExecutablesInDriveAsync(drive, SearchOption.AllDirectories,
            cliContext.CancellationToken);

        try
        {
            // The library owns the readiness decision; its not-ready ArgumentException surfaces here.
            return await ResultHelper.PrintFileSearchResultsAsync(files, Limit).ConfigureAwait(true);
        }
        catch (ArgumentException exception)
        {
            return ResultHelper.PrintException(exception, Verbose, -1);
        }
    }
}