/*
    WhatExec
    Copyright (c) 2025-2026 Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Diagnostics;

namespace WhatExec.Cli.Commands;

[CliCommand(
    Name = "",
    Description = "Locate commands and/or executable files."
)]
public class FindCommand
{
    private readonly IExecutableFileResolver _executableFileResolver;

    public FindCommand(
        IExecutableFileResolver executableFileResolver)
    {
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
    
    [CliOption(Description = "Report time taken to resolve executable files.", Name = "--report-time")]
    [DefaultValue(false)]
    public bool ReportTimeTaken { get; set; }

    private readonly Stopwatch _stopwatch = new();
    
    public int Run()
    {
        if(ReportTimeTaken)
            _stopwatch.Start();
        
        Dictionary<string, FileInfo> commandLocations = new();
        
        if (Limit < 1)
        { 
            Console.WriteLine(Resources.Exceptions_Commands_Find_Limit_MustBeGreaterThanZero);
            return -1;
        }

        if (Commands is null && Interactive)
            Commands = UserInputHelper.GetCommandInput();
        else if (Commands is null)
        {
            Console.WriteLine(Resources.Errors_Commands_NoCommandsSpecified);
            return -1;
        }

        Task<IReadOnlyDictionary<string, FileInfo>> task = Task.Run(() => TrySearchSystem_DoNotLocateAll(
            Commands));
        task.Wait();

        IReadOnlyDictionary<string, FileInfo> nonLocateAllResults = task.Result;

        foreach (KeyValuePair<string, FileInfo> pair in nonLocateAllResults)
        {
            commandLocations[pair.Key] = pair.Value;
        }

        int res = ResultHelper.PrintResults(commandLocations, Commands);
        
        if (ReportTimeTaken)
        {
            _stopwatch.Stop();
            Console.WriteLine(Resources.Commands_Results_ReportTime_Milliseconds, _stopwatch.ElapsedMilliseconds);
        }
        
        return res;
    }

    private IReadOnlyDictionary<string, FileInfo> TrySearchSystem_DoNotLocateAll(
        string[] commandLeftToLookFor)
    {
        _executableFileResolver.TryLocateExecutableFiles(out IReadOnlyDictionary<string, FileInfo> resolvedExecutables,
            SearchOption.AllDirectories, commandLeftToLookFor);
        
        return resolvedExecutables;
    }
}