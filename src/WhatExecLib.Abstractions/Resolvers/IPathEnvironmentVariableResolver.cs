/*
    WhatExecLib
    Copyright (c) 2025-2026 Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

namespace WhatExec.Lib.Abstractions;

/// <summary>
/// Defines methods to resolve file paths for executable files based on the system's PATH environment variable.
/// </summary>
public interface IPathEnvironmentVariableResolver
{
    /// <summary>
    /// Resolves the file path of a file name that is in the PATH Environment Variable.
    /// </summary>
    /// <param name="inputFilePath"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<KeyValuePair<string, FileInfo>> ResolveExecutableFilePathAsync(string inputFilePath,
        CancellationToken cancellationToken);

    /// <summary>
    /// Resolves the file paths of multiple file names that are in the PATH Environment Variable.
    /// </summary>
    /// <param name="inputFilePaths">An array of input file names to resolve against the PATH environment variable.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>An array of resolved <see cref="FileInfo"/> objects containing the file paths of the input file names.</returns>
    /// <exception cref="FileNotFoundException">
    /// Thrown when one or more of the specified file names could not be found in the PATH environment variable.
    /// </exception>
    Task<IReadOnlyDictionary<string, FileInfo>> ResolveAllExecutableFilePathsAsync(string[] inputFilePaths, CancellationToken cancellationToken);

    /// <summary>
    /// Attempts to resolve a file from the system's PATH environment variable using the provided file name.
    /// </summary>
    /// <param name="inputFilePath">The name of the file to resolve, including optional relative or absolute paths.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>True if the file is successfully resolved; otherwise, false.</returns>
    Task<(bool, KeyValuePair<string, FileInfo>?)> TryResolveExecutableAsync(string inputFilePath, CancellationToken cancellationToken);

    /// <summary>
    /// Tries to resolve the file paths for a set of input file names that are in the PATH Environment Variable.
    /// </summary>
    /// <param name="inputFilePaths">An array of file names to resolve.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>True if at least one file path was successfully resolved; otherwise, false.</returns>
    Task<(bool, IReadOnlyDictionary<string, FileInfo>)> TryResolveAllExecutableFilePathsAsync(string[] inputFilePaths, CancellationToken cancellationToken);
}