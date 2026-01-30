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
     
        foreach (KeyValuePair<string, FileInfo[]> pair in task.Result)
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
#if DEBUG
            Console.WriteLine(Resources.LocateExecutable_Status_LookingForCommand, command);
#endif
            FileInfo[] info = _executableFileInstancesResolver.LocateExecutableInstances(
                command,
                SearchOption.AllDirectories
            );
            
            output.Add(command, info);
        }

        return new ReadOnlyDictionary<string, FileInfo[]>(output);
    }
}