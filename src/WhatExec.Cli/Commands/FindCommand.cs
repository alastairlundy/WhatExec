/*
    WhatExec
    Copyright (c) 2025-2026 Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Collections.ObjectModel;

namespace WhatExec.Cli.Commands;

[CliCommand(
    Name = "",
    Description = "Locate commands and/or executable files."
)]
public class FindCommand
{
    private readonly IPathEnvironmentVariableResolver _pathEnvironmentVariableResolver;
    private readonly IExecutableFileInstancesResolver _executableFileInstancesResolver;
    private readonly IExecutableFileResolver _executableFileResolver;

    public FindCommand(
        IPathEnvironmentVariableResolver pathEnvironmentVariableResolver,
        IExecutableFileInstancesResolver executableFileInstancesResolver,
        IExecutableFileResolver executableFileResolver
    )
    {
        _pathEnvironmentVariableResolver = pathEnvironmentVariableResolver;
        _executableFileInstancesResolver = executableFileInstancesResolver;
        _executableFileResolver = executableFileResolver;
    }

    [CliArgument(
        Name = "<Commands or Executable Files>",
        Description = "The commands or executable files to locate."
    )]
    public string[]? Commands { get; set; }

    [CliOption(
        Alias = "--a",
        Name = "--all",
        Description = "Find all instances of the specified Commands and/or Executable Files"
    )]
    [DefaultValue(false)]
    public bool LocaleAllInstances { get; set; }

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

        if (LocaleAllInstances)
            Limit = int.MaxValue;
        
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

        bool foundInPath = _pathEnvironmentVariableResolver.TryResolveAllExecutableFilePaths(
            Commands,
            out IReadOnlyDictionary<string, FileInfo> pathResolvedExecutables
        );
        
        if (foundInPath)
        {
            foreach (KeyValuePair<string, FileInfo> pathSearchResult in pathResolvedExecutables)
            {
                commandLocations[pathSearchResult.Key].Add(pathSearchResult.Value);
            }

            if (!LocaleAllInstances && commandLocations.All(x => x.Value.Count > 0))
            {
                return ResultHelper.PrintResults(commandLocations, Limit);
            }
        }
        else
        {
            Console.WriteLine("Commands were not found in the path environment variable.");
        }

        string[] commandsLeftToLookFor = commandLocations
            .Where(x => x.Value.Count == 0)
            .Select(x => x.Key)
            .ToArray();

        IReadOnlyDictionary<string, FileInfo[]>? locateAllResults = null;
        IReadOnlyDictionary<string, FileInfo>? nonLocateAllResults = null;

        if (LocaleAllInstances)
        {
            Task<IReadOnlyDictionary<string, FileInfo[]>> task = Task.Run(() => TrySearchSystem_LocateAllInstances(commandsLeftToLookFor));
            task.Wait();

            locateAllResults = task.Result; 
        }
        else
        {
            Task<IReadOnlyDictionary<string, FileInfo>> task = Task.Run(() => TrySearchSystem_DoNotLocateAll(
                commandsLeftToLookFor));
            task.Wait();

            nonLocateAllResults = task.Result;
        }

        if (locateAllResults is not null)
        {
            foreach (KeyValuePair<string, FileInfo[]> pair in locateAllResults)
            {
                commandLocations[pair.Key].AddRange(pair.Value);
            }

            return ResultHelper.PrintResults(commandLocations, Limit);
        }
        if (nonLocateAllResults is not null)
        {
            foreach (KeyValuePair<string, FileInfo> pair in nonLocateAllResults)
            {
                commandLocations[pair.Key].Add(pair.Value);
            }

            return ResultHelper.PrintResults(commandLocations, Limit);
        }

        return -1;
    }

    private IReadOnlyDictionary<string, FileInfo> TrySearchSystem_DoNotLocateAll(
        string[] commandLeftToLookFor)
    {
        _executableFileResolver.TryLocateExecutableFiles(out IReadOnlyDictionary<string, FileInfo> resolvedExecutables,
            commandLeftToLookFor);

        return resolvedExecutables;
    }

    private IReadOnlyDictionary<string, FileInfo[]> TrySearchSystem_LocateAllInstances(
        string[] commandsLeftToLookFor)
    {
        Dictionary<string, FileInfo[]> output = new(capacity: commandsLeftToLookFor.Length);

        foreach (string command in commandsLeftToLookFor)
        {
            Console.WriteLine($"Looking for {command}");
            
            FileInfo[] info = _executableFileInstancesResolver.LocateExecutableInstances(
                command,
                SearchOption.AllDirectories
            );
            
            output.Add(command, info);
        }

        return new ReadOnlyDictionary<string, FileInfo[]>(output);
    }
}