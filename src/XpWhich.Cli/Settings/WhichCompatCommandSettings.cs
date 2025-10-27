using System.ComponentModel;
using Spectre.Console.Cli;

namespace XpWhich.Cli.Settings;

public class WhichCompatCommandSettings : CommandSettings
{
    [CommandArgument(0, "<Command(s)>")]
    public string? Commands { get; init; }
    
    [CommandOption("--all|-a")]
    [DefaultValue(false)]
    public bool PrintAllResults { get; init; }
    
    [CommandOption("-s")]
    [DefaultValue(false)]
    public bool OnlyReturnExitCode { get; init; }
    
    [CommandOption("--skip-dot")]
    [DefaultValue(false)]
    public bool SkipDirectoriesStartingWithDot { get; init; }
    
    [CommandOption("--skip-tilde")] 
    [DefaultValue(false)]
    public bool SkipDirectoriesStartingWithTilde { get; init; }
    
    [CommandOption("--show-tilde")]
    [DefaultValue(false)]
    public bool OutputTildeWhenDirectoryMatchesHome { get; init; }
    
    [CommandOption("--show-dot")]
    [DefaultValue(false)]
    public bool OutputDotWhenDirectoryPathStartsWithDot { get; init; }
}