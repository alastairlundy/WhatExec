using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AlastairLundy.WhatExec.Cli.Settings;

public abstract class WhatExecBaseCommandSettings : CommandSettings
{
    [CommandOption("-a|--all")]
    [DefaultValue(false)]
    public bool PrintAllResults { get; set; }

    [CommandOption("-l|--limit")]
    [DefaultValue(3)]
    public int NumberOfResultsToShow { get; init; }

    [CommandOption("--use-caching")]
    [DefaultValue(false)]
    public bool UseCaching { get; init; }

    [CommandOption("--cache-lifetime")]
    public double? CacheLifetimeMinutes { get; set; }

    [CommandOption("-v|--verbose")]
    [DefaultValue(false)]
    public bool ShowErrorsAndBeVerbose { get; init; }

    [CommandOption("--non-interactive")]
    [DefaultValue(false)]
    public bool DisableInteractivity { get; init; }

    public override ValidationResult Validate()
    {
        if (NumberOfResultsToShow < 0)
        {
            ValidationResult.Error(
                "Number of results to show must be greater than or equal to zero."
            );
        }

        if (UseCaching && CacheLifetimeMinutes is null)
            CacheLifetimeMinutes = 3.0;

        return base.Validate();
    }
}
