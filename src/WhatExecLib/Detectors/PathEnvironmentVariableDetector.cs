/*
    WhatExecLib
    Copyright (c) 2025-2026 Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

namespace WhatExecLib.Detectors;

/// <summary>
/// Detects the system's PATH environment variables.
/// </summary>
public class PathEnvironmentVariableDetector : IPathEnvironmentVariableDetector
{
    /// <summary>
    /// Represents the character used to separate individual entries in the PATH environment variable.
    /// The value is ';' on Windows and ':' on non-Windows operating systems.
    /// </summary>
    public char PathContentsSeparatorChar 
        => OperatingSystem.IsWindows() ? ';' : ':';

    /// <summary>
    /// Enumerates the directories listed in the system's PATH environment variable.
    /// </summary>
    /// <returns>
    /// An enumerable collection of strings representing the individual directories in the PATH environment variable,
    /// or null if the PATH variable has not been set.
    /// </returns>
    public IEnumerable<string>? EnumerateDirectories()
    {
        return Environment.GetEnvironmentVariable("PATH")
            ?.Split(PathContentsSeparatorChar, StringSplitOptions.RemoveEmptyEntries)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(x =>
            {
                x = x.Trim();
                x = Environment.ExpandEnvironmentVariables(x);
                x = x.Trim('"');
                const string homeToken = "$HOME";
                string userProfile = Environment.GetFolderPath(
                    Environment.SpecialFolder.UserProfile);

                int homeTokenIndex = x.IndexOf(
                    homeToken,
                    StringComparison.CurrentCultureIgnoreCase
                );

                if (x.StartsWith('~'))
                {
                    x =
                        $"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}{x.Substring(1)}";
                }

                if (homeTokenIndex != -1)
                {
                    return
                        $"{x.Substring(0, homeTokenIndex)}{userProfile}{x.Substring(homeTokenIndex + homeToken.Length)}";
                }

                x = x.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                return x;
            });
    }
    
    /// <summary>
    /// Retrieves the directories listed in the system's PATH environment variable.
    /// </summary>
    /// <returns>
    /// An array of strings representing the individual directories in the PATH environment variable,
    /// or null if the PATH variable has not been set.
    /// </returns>
    public  string[]? GetDirectories() => EnumerateDirectories()?.ToArray();

    /// <summary>
    /// Enumerates the distinct file extensions specified in the system's PATHEXT environment variable
    /// on Windows systems.
    /// </summary>
    /// <returns>
    /// A sequence of strings representing the file extensions from the PATHEXT environment variable,
    /// or a fallback to standard executable extensions if the variable is not defined. On non-Windows systems,
    /// returns a sequence containing a single empty string.
    /// </returns>
    public IEnumerable<string> EnumerateFileExtensions()
    {
        if (!OperatingSystem.IsWindows()) return [""];
        
        return Environment
                   .GetEnvironmentVariable("PATHEXT")
                   ?.Split(PathContentsSeparatorChar, StringSplitOptions.RemoveEmptyEntries)
                   .Where(p => !string.IsNullOrWhiteSpace(p))
                   .Select(x =>
                   {
                       x = x.Trim();
                       x = x.Trim('"');
                       if (!x.StartsWith('.'))
                           x = x.Insert(0, ".");

                       return x;
                   })
                   .Distinct(StringComparer.OrdinalIgnoreCase)
               ?? [".COM", ".EXE", ".BAT", ".CMD"];
    }
    
    /// <summary>
    /// Retrieves the file extensions listed in the system's PATHEXT environment variable specific to Windows systems.
    /// </summary>
    /// <returns>
    /// An array of strings representing the distinct file extensions in the PATHEXT environment variable,
    /// or a fallback to commonly used extensions if the variable is unset. Returns one file extension of "" on non-Windows systems.
    /// </returns>
    public string[] GetPathFileExtensions() 
        => EnumerateFileExtensions().ToArray();
}