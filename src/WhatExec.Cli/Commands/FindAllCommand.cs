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
    
    
    public int Run()
    {
        Dictionary<string, List<FileInfo>> commandLocations = new();

        if (Commands is null && Interactive)
            Commands = UserInputHelper.GetCommandInput();
        else if (Commands is null)
        {
            Console.WriteLine();
            return -1;
        }
        

            Task<IReadOnlyDictionary<string, FileInfo[]>> task = Task.Run(() => TrySearchSystem_LocateAllInstances(Commands));
            task.Wait();

            locateAllResults = task.Result; 
     
            foreach (KeyValuePair<string, FileInfo[]> pair in locateAllResults)
            {
                commandLocations[pair.Key].AddRange(pair.Value);
            }

            return ResultHelper.PrintResults(commandLocations, Limit);
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