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
    Parent = typeof(FindCommand)
)]
public class SearchCommand
{
    private readonly IExecutablesLocator _executablesLocator;
    private readonly IStorageDriveDetector _storageDriveDetector;

    public SearchCommand(IExecutablesLocator executablesLocator, IStorageDriveDetector storageDriveDetector)
    {
        _executablesLocator = executablesLocator;
        _storageDriveDetector = storageDriveDetector;
    }

    [CliOption(
        Name = "--limit",
        Alias = "-l",
        Description = "Limits the number of results returned per command or file."
    )]
    [Range(1, int.MaxValue)]
    public int Limit { get; set; } = 1;
    
    public async Task<int> Run(CancellationToken cancellationToken)
    {
        if (Limit < 1)
        { 
            Console.WriteLine(Resources.Exceptions_Commands_Find_Limit_MustBeGreaterThanZero);
            return -1;
        }
        
        IAsyncEnumerable<FileInfo> files = LocateExecutables(cancellationToken);

        return await ResultHelper.PrintResults(files, Limit);
    }

    private async IAsyncEnumerable<FileInfo> LocateExecutables([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        DriveInfo[] drives = _storageDriveDetector.GetLogicalDrives();

        Task<FileInfo[]>[] tasks = new Task<FileInfo[]>[drives.Length];

        for (int index = 0; index < tasks.Length; index++)
        {
            int index1 = index;
            tasks[index1] =  new Task<FileInfo[]>(() => _executablesLocator.LocateAllExecutablesWithinDrive(drives[index1], 
                SearchOption.AllDirectories), cancellationToken);
            
            tasks[index1].Start();
        }
        
        await foreach (Task<FileInfo[]> resultTask in Task.WhenEach(tasks).WithCancellation(cancellationToken))
        {
            foreach (FileInfo fileInfo in resultTask.Result)
            {
                yield return fileInfo;
            }
        }
    }
}