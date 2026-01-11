/*
    WhatExec
    Copyright (c) 2025 Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Collections.ObjectModel;

namespace WhatExec.Cli.Commands.Find;

[CliCommand(
    Name = "find",
    Description = "Locate commands and/or executable files.",
    Parent = typeof(RootCliCommand)
)]
public class FindCommand
{
    private readonly IPathExecutableResolver _pathExecutableResolver;
    private readonly IExecutableFileInstancesLocator _executableFileInstancesLocator;
    private readonly IMultiExecutableFileLocator _multiExecutableFileLocator;

    public FindCommand(
        IPathExecutableResolver pathExecutableResolver,
        IExecutableFileInstancesLocator executableFileInstancesLocator,
        IMultiExecutableFileLocator multiExecutableFileLocator
    )
    {
        _pathExecutableResolver = pathExecutableResolver;
        _executableFileInstancesLocator = executableFileInstancesLocator;
        _multiExecutableFileLocator = multiExecutableFileLocator;
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
        Description = "Limit the number of results returned per command or file."
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
            AnsiConsole.WriteException(
                new ArgumentOutOfRangeException(
                    nameof(Limit),
                    Resources.Exceptions_Commands_Find_Limit_MustBeGreaterThanZero
                )
            );
        }

        if (Commands is null && Interactive)
            Commands = UserInputHelper.GetCommandInput();
        else if (Commands is null)
        {
            AnsiConsole.WriteException(new ArgumentNullException(nameof(Commands)));
            return -1;
        }

        foreach (string command in Commands)
        {
            commandLocations.Add(command, new List<FileInfo>());
        }

        bool foundInPath = _pathExecutableResolver.TryResolveAllExecutableFilePaths(
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
                return PrintResults(commandLocations);
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

            return PrintResults(commandLocations);
        }
        if (nonLocateAllResults is not null)
        {
            foreach (KeyValuePair<string, FileInfo> pair in nonLocateAllResults)
            {
                commandLocations[pair.Key].Add(pair.Value);
            }

            return PrintResults(commandLocations);
        }

        return -1;
    }

    private int PrintResults(Dictionary<string, List<FileInfo>> results)
    {
        foreach (KeyValuePair<string, List<FileInfo>> result in results)
        {
            IEnumerable<string> allowedResults = result.Value.Take(Limit)
                .Select(f => f.FullName);

            string joinedString = string.Join(Environment.NewLine, allowedResults);

            AnsiConsole.WriteLine(joinedString);
        }

        return 0;
    }

    private IReadOnlyDictionary<string, FileInfo> TrySearchSystem_DoNotLocateAll(
        string[] commandLeftToLookFor)
    {
        _multiExecutableFileLocator.TryLocateExecutableFiles(out IReadOnlyDictionary<string, FileInfo> resolvedExecutables,
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
            
            FileInfo[] info = _executableFileInstancesLocator.LocateExecutableInstances(
                command,
                SearchOption.AllDirectories
            );
            
            output.Add(command, info);
        }

        return new ReadOnlyDictionary<string, FileInfo[]>(output);
    }
}
