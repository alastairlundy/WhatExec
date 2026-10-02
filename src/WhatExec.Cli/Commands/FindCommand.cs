/*
    WhatExec
    Copyright (c) 2025-2026 Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Diagnostics;
using WhatExec.Lib.Locators;
using WhatExec.Lib.Resolvers;

namespace WhatExec.Cli.Commands;

[CliCommand(
    Name = "",
    ShortFormAutoGenerate = CliNameAutoGenerate.None,
    Description = "Locate commands and/or executable files."
)]
public class FindCommand
{
    private readonly IPathEnvironmentVariableResolver _pathResolver;
    private readonly IExecutableInstancesLocator _instancesLocator;

    public FindCommand(
        IPathEnvironmentVariableResolver pathResolver,
        IExecutableInstancesLocator instancesLocator)
    {
        _pathResolver = pathResolver;
        _instancesLocator = instancesLocator;
    }

    [CliArgument(
        Name = "Commands or Executable Files",
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

        StringComparer keyComparer = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        Dictionary<string, FileInfo> commandLocations = new(keyComparer);
        
        if (Limit < 1)
        { 
            await Console.Error.WriteLineAsync(Resources.Exceptions_Commands_Find_Limit_MustBeGreaterThanZero).ConfigureAwait(true);
            return -1;
        }

        if (Commands is null && Interactive)
            Commands = UserInputHelper.GetCommandInput();
        else if (Commands is null)
        {
            await Console.Error.WriteLineAsync(Resources.Errors_Commands_NoCommandsSpecified).ConfigureAwait(true);
            return -1;
        }
        
        IReadOnlyDictionary<string, FileInfo> result = await TrySearchSystem_DoNotLocateAll(
            Commands, cliContext.CancellationToken).ConfigureAwait(true);

        AnsiConsole.Status()
            .Start("Preparing to display results...", _ =>
            {
                foreach (KeyValuePair<string, FileInfo> pair in result)
                {
                    if (!commandLocations.TryAdd(pair.Key, pair.Value))
                        commandLocations[pair.Key] = pair.Value;
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
        StringComparison nameComparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        StringComparer keyComparer = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        try
        {
            return await LocateFirstMatchesPathFirstAsync(commandLeftToLookFor, cancellationToken).ConfigureAwait(true);
        }
        catch(UnauthorizedAccessException unauthorizedAccessException)
        {
            string? problematicCommand = commandLeftToLookFor.FirstOrDefault(command => unauthorizedAccessException.Message.Contains(command, nameComparison));

            if (problematicCommand is null)
            {
                return await LocateFirstMatchesPathFirstAsync(commandLeftToLookFor, cancellationToken).ConfigureAwait(true);
            }

            if (Verbose)
            {
                Console.WriteLine(Resources.Errors_Information_CommandNotLocated
                    .Replace("{0}", problematicCommand)       
                    .Replace("{1}", unauthorizedAccessException.Message));
                Console.WriteLine();
            }
             
            commandLeftToLookFor = commandLeftToLookFor.Where(c => !string.Equals(c, problematicCommand,
                nameComparison)).ToArray();
             
            bool continueInteractive = !Interactive || UserInputHelper.ContinueIfUnauthorizedAccessExceptionOccurs();
             
            if(continueInteractive)
            {
                return await LocateFirstMatchesPathFirstAsync(commandLeftToLookFor, cancellationToken).ConfigureAwait(true);
            }
            else
            {
                return new Dictionary<string, FileInfo>(keyComparer);
            }
        }
        catch (IOException ioException) when (!Verbose)
        {
            commandLeftToLookFor = commandLeftToLookFor.Where(c => !ioException.Message.Contains(c, nameComparison)).ToArray();

            bool continueInteractive = !Interactive || UserInputHelper.ContinueIfUnauthorizedAccessExceptionOccurs();

            if (continueInteractive && commandLeftToLookFor.Length > 0)
            {
                return await LocateFirstMatchesPathFirstAsync(commandLeftToLookFor, cancellationToken).ConfigureAwait(true);
            }

            return new Dictionary<string, FileInfo>(keyComparer);
        }
    }

    /// <summary>
    /// PATH-first composition above both seams (D011): resolves each name against PATH first,
    /// then scans drives for the remainder via the named-instances locator.
    /// Legacy edge-case difference (D010): the retired resolver walked each drive's
    /// directories per name with its own PATHEXT handling, while the shared traversal core
    /// now walks recursively and verdicts every file through the detector - ordering and
    /// completeness of scan hits may differ, but PATH-first precedence is preserved.
    /// </summary>
    private async Task<IReadOnlyDictionary<string, FileInfo>> LocateFirstMatchesPathFirstAsync(
        string[] commandLeftToLookFor, CancellationToken cancellationToken)
    {
        StringComparer keyComparer = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        Dictionary<string, FileInfo> output = new(keyComparer);

        IReadOnlyDictionary<string, FileInfo> pathMatches = await _pathResolver.
            TryGetExecutableFilePathsAsync(commandLeftToLookFor, cancellationToken).ConfigureAwait(true);

        foreach (KeyValuePair<string, FileInfo> match in pathMatches)
        {
            output.TryAdd(match.Key, match.Value);
        }

        foreach (string command in commandLeftToLookFor)
        {
            if (output.ContainsKey(command))
                continue;

            await foreach (FileInfo file in _instancesLocator.EnumerateExecutableInstancesAcrossDrivesAsync(
                               command, SearchOption.AllDirectories, cancellationToken).ConfigureAwait(true))
            {
                output.TryAdd(command, file);
                break;
            }
        }

        return output;
    }
}