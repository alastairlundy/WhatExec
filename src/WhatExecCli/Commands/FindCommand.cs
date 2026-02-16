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
    ShortFormAutoGenerate = CliNameAutoGenerate.None,
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
    
    [CliOption(Name = "--verbose", Alias = "-vb", Description = "Enable verbose output and enhanced error message(s).")]
    [DefaultValue(false)]
    public bool Verbose { get; set; }

    private readonly Stopwatch _stopwatch = new();
    
    public async Task<int> RunAsync(CliContext cliContext)
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
        
        IReadOnlyDictionary<string, FileInfo> result = await TrySearchSystem_DoNotLocateAll(
            Commands, cliContext.CancellationToken);

        AnsiConsole.Status()
            .Start("Preparing to display results...", _ =>
            {
                foreach (KeyValuePair<string, FileInfo> pair in result)
                {
                    commandLocations.Add(pair.Key, pair.Value);
                }
            });

        int res = ResultHelper.PrintResults(commandLocations, Commands);
        
        if (ReportTimeTaken)
        {
            _stopwatch.Stop();
            Console.WriteLine(Resources.Commands_Results_ReportTime_Milliseconds, _stopwatch.ElapsedMilliseconds);
        }
        
        return res;
    }

    private async Task<IReadOnlyDictionary<string, FileInfo>> TrySearchSystem_DoNotLocateAll(
        string[] commandLeftToLookFor, CancellationToken cancellationToken)
    {
        try
        {
            (bool success, IReadOnlyDictionary<string, FileInfo> executableFiles) results =  await _executableFileResolver.
                TryLocateExecutableFilesAsync(commandLeftToLookFor, SearchOption.AllDirectories, cancellationToken);

            return results.executableFiles;
        }
        catch(AggregateException unauthorizedAccessException)
        {
            string? problematicCommand = commandLeftToLookFor.FirstOrDefault(command => unauthorizedAccessException.InnerExceptions.First()
                .Message.Contains(command));

            (bool success, IReadOnlyDictionary<string, FileInfo> resolvedExecutables) results;
            
            if (problematicCommand is null)
            {
                results = await _executableFileResolver.TryLocateExecutableFilesAsync(commandLeftToLookFor,
                    SearchOption.AllDirectories, cancellationToken);
                
                return results.resolvedExecutables;
            }

            if (Verbose)
            {
                Console.WriteLine(Resources.Errors_Information_CommandNotLocated
                    .Replace("{0}", problematicCommand)       
                    .Replace("{1}", unauthorizedAccessException.InnerExceptions.First().Message));
                Console.WriteLine();
            }
            
            commandLeftToLookFor = commandLeftToLookFor.SkipWhile(c => c == problematicCommand).ToArray();
            
            bool continueInteractive = !Interactive || UserInputHelper.ContinueIfUnauthorizedAccessExceptionOccurs();
            
            if(continueInteractive)
            {
                results = await _executableFileResolver.TryLocateExecutableFilesAsync(commandLeftToLookFor, SearchOption.AllDirectories, cancellationToken);
            }
            else
            {
                results = (false, new Dictionary<string, FileInfo>());
            }

            return results.resolvedExecutables;
        }
    }
}