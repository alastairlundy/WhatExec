/*
    WhatExec
    Copyright (c) 2025-2026 Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using DotPrimitives.IO.Drives;

namespace WhatExec.Cli.Commands.Search;

public class SearchDirectoryCommand
{
    private readonly IExecutablesLocator _executablesLocator;
    private readonly IStorageDriveDetector _storageDriveDetector;

    public SearchDirectoryCommand(IExecutablesLocator executablesLocator, 
        IStorageDriveDetector storageDriveDetector)
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

    [CliOption(Description = "Enable interactivity.", Alias = "-i", Name = "--interactive")]
    [DefaultValue(false)]
    public bool Interactive { get; set; }
    
    [CliOption(Name = "--verbose", Description = "Enable verbose exception output.")]
    [DefaultValue(false)]
    public bool Verbose { get; set; }
    
    [CliArgument(Order = 0, Name = "<directory>", Description = "The directory to be searched.")]
    public string Directory { get; set; }
    
    public int Run()
    {
        try
        {
            ArgumentException.ThrowIfNullOrEmpty(Directory);
        }
        catch (Exception)
        {
            if (!Interactive)
                throw;
            
            Directory = UserInputHelper.GetDirectoryInput(new DriveInfo(UserInputHelper.GetDriveInput()));
        }
        
        DirectoryInfo directoryInfo = new DirectoryInfo(Directory);

        if (!directoryInfo.Exists)
            return ResultHelper.PrintException(new DirectoryNotFoundException(Resources.ValidationErrors_Directory_NotSpecified),
                Verbose, -1);
            
        if (Limit < 1)
        { 
            Console.WriteLine(Resources.Exceptions_Commands_Find_Limit_MustBeGreaterThanZero);
            return -1;
        }

        FileInfo[] files = _executablesLocator.LocateAllExecutablesWithinDirectory(directoryInfo,
            SearchOption.AllDirectories);

        return ResultHelper.PrintResults(files, Limit);
    }
}