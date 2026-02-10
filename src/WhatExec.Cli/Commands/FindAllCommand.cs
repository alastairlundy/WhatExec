using System.Collections.ObjectModel;

namespace WhatExec.Cli.Commands;

[CliCommand(
    Name = "all",
    Description = "Locate commands and/or executable files.",
    Parent = typeof(FindCommand)
)]
public class FindAllCommand
{
    private readonly IExecutableFileInstancesResolver _executableFileInstancesResolver;

    public FindAllCommand(IExecutableFileInstancesResolver executableFileInstancesResolver)
    {
        _executableFileInstancesResolver = executableFileInstancesResolver;
    }
    
    [CliOption(Description = "Enable interactivity.", Alias = "-i", Name = "--interactive")]
    [DefaultValue(false)]
    public bool Interactive { get; set; }
    
    
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
    
    public async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        Dictionary<string, List<FileInfo>> commandLocations = new();

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
        
        IReadOnlyDictionary<string, FileInfo[]> result = await TrySearchSystem_LocateAllInstances(Commands, cancellationToken);
     
        foreach (KeyValuePair<string, FileInfo[]> pair in result)
        {
            commandLocations[pair.Key].AddRange(pair.Value);
        }

        return ResultHelper.PrintResults(commandLocations, Commands, Limit);
    }
    
    private async Task<IReadOnlyDictionary<string, FileInfo[]>> TrySearchSystem_LocateAllInstances(
        string[] commandsLeftToLookFor, CancellationToken cancellationToken)
    {
        Dictionary<string, FileInfo[]> output = new(capacity: commandsLeftToLookFor.Length);

        foreach (string command in commandsLeftToLookFor)
        {
            try
            {
                FileInfo[] info = await _executableFileInstancesResolver.LocateExecutableInstancesAsync(
                    command,
                    SearchOption.AllDirectories,
                    cancellationToken
                );
            
                output.Add(command, info);
            }
            catch (AggregateException)
            {
                // Skip and move to the next executable.
            }
        }

        return new ReadOnlyDictionary<string, FileInfo[]>(output);
    }
}