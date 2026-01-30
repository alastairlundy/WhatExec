/*
    WhatExec
    Copyright (c) 2025-2026 Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

namespace WhatExec.Cli.Commands;

[CliCommand(
    Name = "",
    Description = "Locate commands and/or executable files."
)]
public class FindCommand
{
    private readonly IExecutableFileInstancesResolver _executableFileInstancesResolver;
    private readonly IExecutableFileResolver _executableFileResolver;

    public FindCommand(
        IExecutableFileInstancesResolver executableFileInstancesResolver,
        IExecutableFileResolver executableFileResolver
    )
    {
        _executableFileInstancesResolver = executableFileInstancesResolver;
        _executableFileResolver = executableFileResolver;
    }

    [CliArgument(
        Name = "<Commands or Executable Files>",
        Description = "The commands or executable files to locate."
    )]
    public string[]? Commands { get; set; }
    
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

    public int Run()
    {
        Dictionary<string, List<FileInfo>> commandLocations = new();
        
        if (Limit < 1)
        { 
            Console.WriteLine(Resources.Exceptions_Commands_Find_Limit_MustBeGreaterThanZero);
            return -1;
        }

        if (Commands is null && Interactive)
            Commands = UserInputHelper.GetCommandInput();
        else if (Commands is null)
        {
            Console.WriteLine();
            return -1;
        }

        foreach (string command in Commands)
        {
            commandLocations.Add(command, new List<FileInfo>());
        }

        IReadOnlyDictionary<string, FileInfo>? nonLocateAllResults = null;
        
            Task<IReadOnlyDictionary<string, FileInfo>> task = Task.Run(() => TrySearchSystem_DoNotLocateAll(
                Commands));
            task.Wait();

            nonLocateAllResults = task.Result;

        foreach (KeyValuePair<string, FileInfo> pair in nonLocateAllResults)
        {
            commandLocations[pair.Key].Add(pair.Value);
        }

        return ResultHelper.PrintResults(commandLocations, Limit);
    }

    private IReadOnlyDictionary<string, FileInfo> TrySearchSystem_DoNotLocateAll(
        string[] commandLeftToLookFor)
    {
        Console.WriteLine($"Looking for executables");
        
        _executableFileResolver.TryLocateExecutableFiles(out IReadOnlyDictionary<string, FileInfo> resolvedExecutables,
            SearchOption.AllDirectories, commandLeftToLookFor);

        foreach (KeyValuePair<string, FileInfo> pair in resolvedExecutables)
        {
            Console.WriteLine($"Found executable: {pair.Key} at {pair.Value.FullName}");
        }
        
        return resolvedExecutables;
    }
}