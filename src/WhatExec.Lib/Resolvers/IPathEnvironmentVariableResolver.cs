/*
    WhatExec.Lib
    Copyright (c) 2025-2026 Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

namespace WhatExec.Lib.Resolvers;

/// <summary>
/// Defines methods to resolve file paths for executable files based on the system's PATH environment variable.
/// </summary>
public interface IPathEnvironmentVariableResolver
{
    /// <summary>
    /// Enumerates the file paths of the specified executable names if they are found in the PATH Environment Variable,
    /// yielding the first PATH match per name.
    /// </summary>
    /// <param name="executableNames">The collection of executable names to resolve against the PATH environment variable.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to use to request cancellation.</param>
    /// <returns>An asynchronous sequence of resolved <see cref="KeyValuePair{TKey,TValue}"/> pairs mapping each input name to its located file.</returns>
    IAsyncEnumerable<KeyValuePair<string, FileInfo>> EnumerateExecutableFilePathsAsync(
        IEnumerable<string> executableNames, CancellationToken cancellationToken);

    /// <summary>
    /// Enumerates the file path of a single executable name if found in the PATH Environment Variable,
    /// yielding the first PATH match.
    /// </summary>
    /// <param name="executableName">The executable name to resolve against the PATH environment variable.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to use to request cancellation.</param>
    /// <returns>An asynchronous sequence of resolved <see cref="KeyValuePair{TKey,TValue}"/> pairs mapping the input name to its located file.</returns>
    IAsyncEnumerable<KeyValuePair<string, FileInfo>> EnumerateExecutableFilePathsAsync(
        string executableName, CancellationToken cancellationToken);

    /// <summary>
    /// Attempts to resolve a set of executable names from the system's PATH environment variable,
    /// returning one match per name (first PATH match).
    /// </summary>
    /// <param name="executableNames">The collection of executable names to resolve.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to use to request cancellation.</param>
    /// <returns>A read-only dictionary mapping each resolved name to its located <see cref="FileInfo"/>.</returns>
    Task<IReadOnlyDictionary<string, FileInfo>> TryGetExecutableFilePathsAsync(
        IEnumerable<string> executableNames, CancellationToken cancellationToken);

    /// <summary>
    /// Attempts to resolve a single executable name from the system's PATH environment variable.
    /// </summary>
    /// <param name="executableName">The executable name to resolve.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to use to request cancellation.</param>
    /// <returns>A read-only dictionary mapping the resolved name to its located <see cref="FileInfo"/>,
    /// or an empty dictionary if not found.</returns>
    Task<IReadOnlyDictionary<string, FileInfo>> TryGetExecutableFilePathsAsync(
        string executableName, CancellationToken cancellationToken);
}
