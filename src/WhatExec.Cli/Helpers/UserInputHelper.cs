/*
    WhatExec
    Copyright (c) 2025-2026 Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using ValidationResult = Spectre.Console.ValidationResult;

namespace WhatExec.Cli.Helpers;

internal static class UserInputHelper
{
    internal static bool ContinueIfUnauthorizedAccessExceptionOccurs()
    {
        ConfirmationPrompt prompt = new ConfirmationPrompt(Resources.Prompts_Errors_ContinueAfterError)
            .ShowChoices();
        
        return AnsiConsole.Prompt(prompt);
    }
    
    internal static string GetDirectoryInput(DriveInfo drive)
    {
        IEnumerable<string> directories = drive
            .RootDirectory.SafelyGetDirectories()
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
        string[] drives = DriveInfo.SafelyEnumerateLogicalDrives().Select(d => d.Name).ToArray();

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

    internal static string[] GetCommandInput()
    {
        bool keepAddingCommands;

        List<string> commands = [];

        do
        {
            string command = AnsiConsole.Prompt(
                new TextPrompt<string>(
                        Resources.Prompts_TextInput_File,
                        StringComparer.CurrentCulture)
                    .InvalidChoiceMessage("")
                    .Validate(s =>
                    {
                        if (string.IsNullOrWhiteSpace(s) || string.IsNullOrEmpty(s))
                            return ValidationResult.Error(
                                Resources.ValidationErrors_File_EmptyOrWhitespace
                            );

                        if (s.Contains(Path.DirectorySeparatorChar)
                            || s.Contains(Path.AltDirectorySeparatorChar))
                        {
                            return ValidationResult.Error(
                                Resources.ValidationErrors_File_CannotContainDirectorySeparator
                            );
                        }

                        return ValidationResult.Success();
                    })
            );

            commands.Add(command);

            keepAddingCommands = AnsiConsole.Prompt(
                new ConfirmationPrompt(Resources.Prompts_CommandInput_AddAnother).ShowChoices()
            );
        } while (keepAddingCommands);

        return commands.ToArray();
    }
}
