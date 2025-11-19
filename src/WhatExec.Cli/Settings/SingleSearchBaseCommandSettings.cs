using System.Linq;

namespace AlastairLundy.WhatExec.Cli.Settings;

public abstract class SingleSearchBaseCommandSettings : CommandSettings
{
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

    [CommandArgument(0, "<File or Command>")]
    public string[]? Commands { get; set; }

    public override ValidationResult Validate()
    {
        if (UseCaching && CacheLifetimeMinutes is null)
            CacheLifetimeMinutes = 3.0;

        if (Commands is null)
        {
            if (!DisableInteractivity)
            {
                Commands ??= UserInputHelper.GetCommandInput();
            }
            else if (DisableInteractivity)
            {
                return ValidationResult.Error(Resources.ValidationErrors_File_NotSpecified);
            }
        }
        else
        {
            Commands = Commands
                .Where(s => !string.IsNullOrWhiteSpace(s) || !string.IsNullOrEmpty(s))
                .ToArray();
        }

        return base.Validate();
    }
}
