/*
    WhatExec
    Copyright (c) 2025-2026 Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Runtime.CompilerServices;

namespace WhatExec.Cli.Commands.Search;

[CliCommand(
    Name = "search",
    Description = "Locate all commands and/or executable files on a system.",
    ShortFormAutoGenerate = CliNameAutoGenerate.None,
    Parent = typeof(FindCommand)
)]
public class SearchCommand
{
    private readonly IExecutablesResolver _executablesResolver;
    private readonly IStorageDriveDetector _storageDriveDetector;

    public SearchCommand(IExecutablesResolver executablesResolver, IStorageDriveDetector storageDriveDetector)
    {
        _executablesResolver = executablesResolver;
        _storageDriveDetector = storageDriveDetector;
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

        return await ResultHelper.PrintFileSearchResultsAsync(files, Limit);
    }

    private async IAsyncEnumerable<FileInfo> LocateExecutables([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        DriveInfo[] drives = _storageDriveDetector.GetLogicalDrives();

        foreach (DriveInfo drive in drives)
        {
            IAsyncEnumerable<FileInfo> driveFiles = _executablesResolver.LocateAllExecutablesWithinDriveAsync(drive, 
                SearchOption.AllDirectories, cancellationToken);

            await foreach (FileInfo file in driveFiles)
            {
                yield return file;
            }
        }
    }
}