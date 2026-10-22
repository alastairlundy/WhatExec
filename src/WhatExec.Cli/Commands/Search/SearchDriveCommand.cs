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
    Parent = typeof(SearchCommand)
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

    [CliOption(Description = "Enable interactivity.", Alias = "-i", Name = "--interactive")]
    [DefaultValue(false)]
    public bool Interactive { get; set; }
    
    [CliArgument(Order = 0, Name = "<drive>", Description = "Locate the drive to search for.")]
    public string? Drive { get; set; }

    [CliOption(Name = "--verbose", Description = "Enable verbose exception output.")]
    [DefaultValue(false)]
    public bool Verbose { get; set; }
    
    public async Task<int> RunAsync(CliContext cliContext)
    {
        DriveInfo drive;

        try
        {
            // The DriveInfo constructor normalises a bare drive letter and a root path to the
            // same drive, so the CLI applies no drive name matching of its own.
            drive = new DriveInfo(Drive!);
        }
        catch (ArgumentException)
        {
            if (!Interactive)
                return ResultHelper.PrintException(new DriveNotFoundException(Resources.ValidationErrors_Drive_NotSpecified),
                    Verbose, -1);

            drive = new DriveInfo(UserInputHelper.GetDriveInput());
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
