/*
    WhatExec
    Copyright (c) 2025 Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.ComponentModel.DataAnnotations;
using DotMake.CommandLine;

namespace WhatExec.Cli.Commands.Find;

[CliCommand(
    Name = "find",
    Description = "Locate commands and/or executable files.",
    Parent = typeof(RootCliCommand)
)]
public class FindCommand
{
    private readonly IPathExecutableResolver _pathExecutableResolver;
    private readonly ICachedPathExecutableResolver _cachedPathExecutableResolver;
    private readonly IExecutableFileInstancesLocator _executableFileInstancesLocator;
    private readonly IExecutableFileLocator _executableFileLocator;

    public FindCommand(
        IPathExecutableResolver pathExecutableResolver,
        ICachedPathExecutableResolver cachedPathExecutableResolver,
        IExecutableFileInstancesLocator executableFileInstancesLocator,
        IExecutableFileLocator executableFileLocator
    )
    {
        _pathExecutableResolver = pathExecutableResolver;
        _cachedPathExecutableResolver = cachedPathExecutableResolver;
        _executableFileInstancesLocator = executableFileInstancesLocator;
        _executableFileLocator = executableFileLocator;
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
        Dictionary<string, List<string>> commandLocations = new();

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
            commandLocations.Add(command, new List<string>());
        }

        bool foundInPath = TrySearchPath(out KeyValuePair<string, string>[]? pathSearchResults);

        if (foundInPath && pathSearchResults is not null)
        {
            foreach (KeyValuePair<string, string> pathSearchResult in pathSearchResults)
            {
                commandLocations[pathSearchResult.Key].Add(pathSearchResult.Value);
            }

            if (!LocaleAllInstances)
            {
                return PrintResults(commandLocations);
            }
        }

        KeyValuePair<string, string>[]? locateAllResults = null;
        KeyValuePair<string, string>[]? nonLocateAllResults = null;

        if (LocaleAllInstances)
        {
            locateAllResults = TrySearchSystem_LocateAllInstances();
        }
        else
        {
            Task<KeyValuePair<string, string>[]?> task = TrySearchSystem_DoNotLocateAll();

            task.Wait();

            nonLocateAllResults = task.Result;
        }

        if (locateAllResults is not null)
        {
            foreach (KeyValuePair<string, string> pair in locateAllResults)
            {
                commandLocations[pair.Key].Add(pair.Value);
            }

            return PrintResults(commandLocations);
        }
        if (nonLocateAllResults is not null)
        {
            foreach (KeyValuePair<string, string> pair in nonLocateAllResults)
            {
                commandLocations[pair.Key].Add(pair.Value);
            }

            return PrintResults(commandLocations);
        }

        return -1;
    }

    private int PrintResults(Dictionary<string, List<string>> results)
    {
        foreach (KeyValuePair<string, List<string>> result in results)
        {
            IEnumerable<string> allowedResults = result.Value.Take(Limit);

            string joinedString = string.Join(Environment.NewLine, allowedResults);

            AnsiConsole.WriteLine(joinedString);
        }

        return 0;
    }

    private bool TrySearchPath(out KeyValuePair<string, string>[]? results)
    {
        if (Commands is null)
        {
            results = null;
            return false;
        }

        List<KeyValuePair<string, string>> output = new(capacity: Commands.Length);

        if (Commands.Length > 1)
        {
            foreach (string command in Commands)
            {
                bool success =
                    _cachedPathExecutableResolver.TryResolvePathEnvironmentExecutableFile(
                        command,
                        out FileInfo? info
                    );

                if (success && info is not null)
                    output.Add(new KeyValuePair<string, string>(command, info.FullName));
            }
        }
        else
        {
            bool success = _pathExecutableResolver.TryResolvePathEnvironmentExecutableFile(
                Commands.First(),
                out FileInfo? info
            );

            if (success && info is not null)
                output.Add(new KeyValuePair<string, string>(Commands.First(), info.FullName));
        }

        results = output.ToArray();
        return output.Count > 0;
    }

    private async Task<KeyValuePair<string, string>[]?> TrySearchSystem_DoNotLocateAll()
    {
        List<KeyValuePair<string, string>> output = new();

        if (Commands is null)
        {
            return null;
        }

        foreach (string command in Commands)
        {
            FileInfo? info = await _executableFileLocator.LocateExecutableAsync(
                command,
                SearchOption.AllDirectories,
                CancellationToken.None
            );

            if (info is not null)
            {
                output.Add(new KeyValuePair<string, string>(command, info.FullName));
            }
        }

        return output.ToArray();
    }

    private KeyValuePair<string, string>[]? TrySearchSystem_LocateAllInstances()
    {
        List<KeyValuePair<string, string>> output = new();

        if (Commands is null)
        {
            return null;
        }

        foreach (string command in Commands)
        {
            IEnumerable<FileInfo> info = _executableFileInstancesLocator
                .LocateExecutableInstances(command, SearchOption.AllDirectories)
                .AsParallel();

            foreach (FileInfo file in info)
            {
                output.Add(new KeyValuePair<string, string>(command, file.FullName));
            }
        }

        return output.ToArray();
    }
}
