/*
    WhatExec
    Copyright (c) 2025-2026 Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using WhatExec.Lib.Abstractions;

namespace WhatExec.Cli.Commands.Search;

[CliCommand(
    Name = "drive",
    Description = "Locate all commands and/or executable files on a Storage Drive.",
    Parent = typeof(SearchCommand)
)]
public class SearchDriveCommand
{
    private readonly IExecutablesResolver _executablesResolver;

    public SearchDriveCommand(IExecutablesResolver executablesResolver)
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
        try
        {
            ArgumentException.ThrowIfNullOrEmpty(Drive);
        }
        catch (Exception)
        {
            if (!Interactive)
                throw;

            Drive = UserInputHelper.GetDriveInput();
        }
        
        DriveInfo? drive = DriveInfo.SafelyEnumerateLogicalDrives().FirstOrDefault(d => d.Name == Drive);
        
        if(drive is null)
            return ResultHelper.PrintException(new DriveNotFoundException(Resources.ValidationErrors_Drive_NotSpecified),
                Verbose, -1);
        
        if (Limit < 1)
        { 
            Console.WriteLine(Resources.Exceptions_Commands_Find_Limit_MustBeGreaterThanZero);
            return -1;
        }

        IAsyncEnumerable<FileInfo> files = _executablesResolver.LocateAllExecutablesWithinDriveAsync(drive, SearchOption.AllDirectories,
            cliContext.CancellationToken);
        
        return await ResultHelper.PrintFileSearchResultsAsync(files, Limit);
    }
}