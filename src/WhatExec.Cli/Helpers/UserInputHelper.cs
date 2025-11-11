using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AlastairLundy.WhatExec.Cli.Localizations;
using AlastairLundy.WhatExec.Cli.Localizations;
using AlastairLundy.WhatExec.Cli.Localizations;
using Spectre.Console;

namespace AlastairLundy.WhatExec.Cli.Helpers;

internal static class UserInputHelper
{
    internal static string GetDirectoryInput(DriveInfo drive)
    {
        IEnumerable<string> directories = drive
            .RootDirectory.GetDirectories()
            .Select(d => d.FullName);

        string directory = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title(
                    Resources.Prompts_Selection_Directory.Replace(
                        Resources.Prompts_Highlights_Directory,
                        $"[underline italic gold1]{Resources.Prompts_Highlights_Directory}"
                    )
                )
                .PageSize(10)
                .AddChoices(directories)
                .MoreChoicesText($"({Resources.Prompts_Selection_RevealMoreOptions})")
        );

        return directory;
    }

    internal static string GetDriveInput()
    {
        string[] drives = Environment.GetLogicalDrives();

        string drive = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title(
                    Resources.Prompts_Selection_StorageDrive.Replace(
                        Resources.Prompts_Highlights_StorageDrive,
                        $"[underline italic gold1]{Resources.Prompts_Highlights_StorageDrive}"
                    )
                )
                .PageSize(10)
                .AddChoices(drives)
                .MoreChoicesText($"({Resources.Prompts_Selection_RevealMoreOptions})")
        );

        return drive;
    }

    internal static string GetFileInput()
    {
        string file = AnsiConsole.Prompt(
            new TextPrompt<string>(Resources.Prompts_TextInput_File, StringComparer.CurrentCulture)
                .InvalidChoiceMessage("")
                .Validate(s =>
                {
                    if (string.IsNullOrWhiteSpace(s) || string.IsNullOrEmpty(s))
                        return ValidationResult.Error(
                            Resources.ValidationErrors_File_EmptyOrWhitespace
                        );

                    if (
                        s.Contains(Path.DirectorySeparatorChar)
                        || s.Contains(Path.AltDirectorySeparatorChar)
                    )
                    {
                        return ValidationResult.Error(
                            Resources.ValidationErrors_File_CannotContainDirectorySeparator
                        );
                    }

                    return ValidationResult.Success();
                })
        );

        return file;
    }
}
