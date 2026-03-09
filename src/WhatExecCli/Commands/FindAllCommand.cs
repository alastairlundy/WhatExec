using System.Collections.ObjectModel;
using WhatExec.Lib.Abstractions;

namespace WhatExec.Cli.Commands;

[CliCommand(
    Name = "all",
    ShortFormAutoGenerate = CliNameAutoGenerate.None,
    Description = "Locate commands and/or executable files.",
    Parent = typeof(FindCommand)
)]
public class FindAllCommand
{
    private readonly IExecutableFileInstancesLocator _executableFileInstancesResolver;

    public FindAllCommand(IExecutableFileInstancesLocator executableFileInstancesResolver)
    {
        _executableFileInstancesResolver = executableFileInstancesResolver;
        
        _executableFileInstancesResolver.ExecutableFileInstanceLocated += ExecutableFileInstancesResolverOnExecutableFileLocated;
    }

    private void ExecutableFileInstancesResolverOnExecutableFileLocated(object? sender, FileInfo e)
    {
        
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
                FileInfo[] info = await _executableFileInstancesResolver.GetExecutableInstancesAsync(
                    command,
                    SearchOption.AllDirectories,
                    cancellationToken
                ).ConfigureAwait(true);
            
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