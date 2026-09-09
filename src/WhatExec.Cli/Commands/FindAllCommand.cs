using System.Collections.ObjectModel;
using WhatExec.Lib.Locators;

namespace WhatExec.Cli.Commands;

[CliCommand(
    Name = "all",
    ShortFormAutoGenerate = CliNameAutoGenerate.None,
    Description = "Locate commands and/or executable files.",
    Parent = typeof(FindCommand)
)]
public class FindAllCommand
{
    private readonly IExecutableInstancesLocator _executableInstancesLocator;

    public FindAllCommand(IExecutableInstancesLocator executableInstancesLocator)
    {
        _executableInstancesLocator = executableInstancesLocator;
    }

    [CliOption(Description = "Enable interactivity.", Alias = "-i", Name = "--interactive")]
    [DefaultValue(false)]
    public bool Interactive { get; set; }
    
    
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
    
    public async Task<int> RunAsync(CliContext cliContext)
    {
        Dictionary<string, List<FileInfo>> commandLocations = new(
            StringComparer.OrdinalIgnoreCase);

        if (Commands is null && Interactive)
            Commands = UserInputHelper.GetCommandInput();
        else if (Commands is null)
        {
            Console.WriteLine();
            return -1;
        }
        
        // Populate command Keys in Dictionary.
        foreach (string command in Commands)
        {
            commandLocations.Add(command, new List<FileInfo>());
        }
        
        IReadOnlyDictionary<string, FileInfo[]> result = await TrySearchSystem_LocateAllInstances(Commands, cliContext.CancellationToken).ConfigureAwait(true);
     
        foreach (KeyValuePair<string, FileInfo[]> pair in result)
        {
            commandLocations[pair.Key].AddRange(pair.Value);
        }

        return ResultHelper.PrintResults(commandLocations, Commands, Limit);
    }
    
    private async Task<IReadOnlyDictionary<string, FileInfo[]>> TrySearchSystem_LocateAllInstances(
        string[] commandsLeftToLookFor, CancellationToken cancellationToken)
    {
        Dictionary<string, FileInfo[]> output = new(capacity: commandsLeftToLookFor.Length, 
            StringComparer.OrdinalIgnoreCase);

        foreach (string command in commandsLeftToLookFor)
        {
            try
            {
                // Named-instances scan only (D019): locate-all enumerates every drive
                // through the shared traversal core. Legacy edge-case difference (D010):
                // scan hits are now verdict-filtered per file by the detector and
                // inaccessible entries are skipped, so instance sets may differ from
                // the retired seam where unauthorized entries surfaced as errors.
                List<FileInfo> instances = new();

                await foreach (FileInfo file in _executableInstancesLocator.EnumerateExecutableInstancesAcrossDrivesAsync(
                                   command, SearchOption.AllDirectories, cancellationToken).ConfigureAwait(true))
                {
                    instances.Add(file);
                }

                output.Add(command, instances.ToArray());
            }
            catch (AggregateException)
            {
                // Skip and move to the next executable.
            }
        }

        return new ReadOnlyDictionary<string, FileInfo[]>(output);
    }
}