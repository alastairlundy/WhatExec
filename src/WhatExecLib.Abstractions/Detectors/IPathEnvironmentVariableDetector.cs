/*
    WhatExecLib
    Copyright (c) 2025-2026 Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

namespace WhatExec.Lib.Abstractions.Detectors;

/// <summary>
/// Represents an interface for detecting PATH environment variables.
/// </summary>
public interface IPathEnvironmentVariableDetector
{
    /// <summary>
    /// Represents the character used to separate individual entries in the PATH environment variable.
    /// The value is ';' on Windows and ':' on non-Windows operating systems.
    /// </summary>
    char PathContentsSeparatorChar { get; }

    /// <summary>
    /// Enumerates the directories listed in the system's PATH environment variable.
    /// </summary>
    /// <returns>
    /// An enumerable collection of strings representing the individual directories in the PATH environment variable,
    /// or null if the PATH variable has not been set.
    /// </returns>
    IEnumerable<string>? EnumerateDirectories();

    /// <summary>
    /// Retrieves the directories listed in the system's PATH environment variable.
    /// </summary>
    /// <returns>
    /// An array of strings representing the individual directories in the PATH environment variable,
    /// or null if the PATH variable has not been set.
    /// </returns>
    string[]? GetDirectories();

    /// <summary>
    /// Enumerates the distinct file extensions specified in the system's PATHEXT environment variable
    /// on Windows systems.
    /// </summary>
    /// <returns>
    /// A sequence of strings representing the file extensions from the PATHEXT environment variable,
    /// or a fallback to standard executable extensions if the variable is not defined. On non-Windows systems,
    /// returns a sequence containing a single empty string.
    /// </returns>
    IEnumerable<string> EnumerateFileExtensions();

    /// <summary>
    /// Retrieves the file extensions listed in the system's PATHEXT environment variable specific to Windows systems.
    /// </summary>
    /// <returns>
    /// An array of strings representing the distinct file extensions in the PATHEXT environment variable,
    /// or a fallback to commonly used extensions if the variable is unset. Returns one file extension of "" on non-Windows systems.
    /// </returns>
    string[] GetPathFileExtensions();
}