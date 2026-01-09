/*
    WhatExec
    Copyright (c) 2025 Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

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
    private readonly IExecutableFileLocator _executableFileLocator;

    public FindCommand(
        IPathExecutableResolver pathExecutableResolver,
        IExecutableFileInstancesLocator executableFileInstancesLocator,
        IExecutableFileLocator executableFileLocator
    )
    {
        _pathExecutableResolver = pathExecutableResolver;
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

            if (!LocaleAllInstances && commandLocations.All(x => x.Value.Count > 0))
            {
                return PrintResults(commandLocations);
            }
        }

        string[] commandsLeftToLookFor = commandLocations
            .Where(x => x.Value.Count == 0)
            .Select(x => x.Key)
            .ToArray();

        KeyValuePair<string, string>[]? locateAllResults = null;
        KeyValuePair<string, string>[]? nonLocateAllResults = null;

        if (LocaleAllInstances)
        {
            locateAllResults = TrySearchSystem_LocateAllInstances(commandsLeftToLookFor);
        }
        else
        {
            Task<KeyValuePair<string, string>[]> task = TrySearchSystem_DoNotLocateAll(
                commandsLeftToLookFor
            );
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

        bool success = _pathExecutableResolver.TryResolveExecutables(
            Commands,
            out FileInfo[]? fileInfos
        );

        if (success && fileInfos is not null)
        {
            foreach (FileInfo info in fileInfos)
            {
                output.Add(
                    new KeyValuePair<string, string>(
                        Commands.FirstOrDefault(c => info.Name == c)
                            ?? Path.GetFileNameWithoutExtension(info.Name),
                        info.FullName
                    )
                );
            }
        }

        results = output.ToArray();
        return output.Count > 0;
    }

    private async Task<KeyValuePair<string, string>[]> TrySearchSystem_DoNotLocateAll(
        string[] commandLeftToLookFor
    )
    {
        List<KeyValuePair<string, string>> output = new();

        foreach (string command in commandLeftToLookFor)
        {
            Console.WriteLine($"Looking for {command}");

            FileInfo? info = await _executableFileLocator.LocateExecutableAsync(
                command,
                SearchOption.AllDirectories,
                CancellationToken.None
            );

            if (info is not null)
            {
                output.Add(new KeyValuePair<string, string>(command, info.FullName));
                Console.WriteLine($"Result for {command} was {info.FullName}");
            }
            else
            {
                Console.WriteLine($"Result for {command} was null");
            }
        }

        return output.ToArray();
    }

    private KeyValuePair<string, string>[] TrySearchSystem_LocateAllInstances(
        string[] commandsLeftToLookFor
    )
    {
        List<KeyValuePair<string, string>> output = new();

        foreach (string command in commandsLeftToLookFor)
        {
            Console.WriteLine($"Looking for {command}");

            IEnumerable<FileInfo> info = _executableFileInstancesLocator.LocateExecutableInstances(
                command,
                SearchOption.AllDirectories
            );

            foreach (FileInfo file in info)
            {
                output.Add(new KeyValuePair<string, string>(command, file.FullName));
            }
        }

        return output.ToArray();
    }
}
