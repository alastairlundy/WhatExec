/*
    WhatExec.Lib
    Copyright (c) 2025-2026 Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

namespace WhatExec.Lib.Abstractions.Resolvers;

/// <summary>
/// Defines methods to resolve file paths for executable files based on the system's PATH environment variable.
/// </summary>
public interface IPathEnvironmentVariableResolver
{
    /// <summary>
    /// Represents the file location of an executable that has been located based on the system's PATH environment variable.
    /// </summary>
    event EventHandler<KeyValuePair<string, FileInfo>> ExecutableFileLocated;
    
    /// <summary>
    /// Resolves the file path of a file name that is in the PATH Environment Variable.
    /// </summary>
    /// <param name="inputFilePath">The name of the file to resolve, including optional relative or absolute paths.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to use to request cancellation.</param>
    /// <returns>A <see cref="KeyValuePair"/> indicating the input file path and the resolved file as a <see cref="FileInfo"/> object.</returns>
    Task<KeyValuePair<string, FileInfo>> ResolveExecutableFilePathAsync(string inputFilePath,
        CancellationToken cancellationToken);

    /// <summary>
    /// Attempts to resolve a file from the system's PATH environment variable using the provided file name.
    /// </summary>
    /// <param name="inputFilePath">The name of the file to resolve, including optional relative or absolute paths.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to use to request cancellation.</param>
    /// <returns>A tuple that contains a bool, which is true if the file is successfully resolved; otherwise, false, and a key value pair of the input file path and resolved file as a <see cref="FileInfo"/> if found
    /// or null if not found.</returns>
    Task<(bool, KeyValuePair<string, FileInfo>?)> TryResolveExecutableFilePathAsync(string inputFilePath, CancellationToken cancellationToken);
    
    /// <summary>
    /// Enumerates the file paths of the specified file names if they are found in the PATH Environment Variable.
    /// </summary>
    /// <param name="inputFilePaths">An array of input file names to resolve against the PATH environment variable.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to use to request cancellation.</param>
    /// <returns>An asynchronous sequence of resolved <see cref="FileInfo"/> objects containing the resolved file paths of the input file names.</returns>
    IAsyncEnumerable<KeyValuePair<string, FileInfo>> EnumerateExecutableFilePathsAsync(string[] inputFilePaths, CancellationToken cancellationToken);
    
    /// <summary>
    /// Gets the file paths of the specified file names if they are found in the PATH Environment Variable.
    /// </summary>
    /// <param name="inputFilePaths">An array of input file names to resolve against the PATH environment variable.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to use to request cancellation.</param>
    /// <returns>An array of resolved <see cref="FileInfo"/> objects containing the resolved file paths of the input file names.</returns>
    /// <exception cref="FileNotFoundException">
    /// Thrown when one or more of the specified file names could not be found in the PATH environment variable.
    /// </exception>
    Task<IReadOnlyDictionary<string, FileInfo>> GetExecutableFilePathsAsync(string[] inputFilePaths, CancellationToken cancellationToken);
    
    /// <summary>
    /// Tries to resolve the file paths for a set of input file names that are in the PATH Environment Variable.
    /// </summary>
    /// <param name="inputFilePaths">An array of file names to resolve.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to use to request cancellation.</param>
    /// <returns>True if at all file paths were successfully resolved; otherwise, false.</returns>
    Task<(bool, IReadOnlyDictionary<string, FileInfo>)> TryGetExecutableFilePathsAsync(string[] inputFilePaths, CancellationToken cancellationToken);
}