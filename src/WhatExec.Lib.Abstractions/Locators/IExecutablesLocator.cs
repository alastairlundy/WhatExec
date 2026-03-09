/*
    WhatExec.Lib
    Copyright (c) 2025-2026 Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

namespace WhatExec.Lib.Abstractions.Locators;

/// <summary>
/// Defines an interface for locating all executable files within a specified directory or drive.
/// </summary>
public interface IExecutablesLocator
{
    /// <summary>
    /// An event that is triggered each time an executable file was located during the resolution process.
    /// </summary>
    event EventHandler<FileInfo> ExecutableFileLocated;
    
    /// <summary>
    /// Enumerates all executable files within a specified directory asynchronously.
    /// </summary>
    /// <param name="directory">The directory in which to search for executable files.</param>
    /// <param name="directorySearchOption">Specifies whether to search all directories or only the top-level directory.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to use to request cancellation.</param>
    /// <returns>An asynchronous sequence of <see cref="FileInfo"/> objects representing the executable files within the specified directory.</returns>
    /// <exception cref="DirectoryNotFoundException">Thrown when the specified directory does not exist.</exception>
    IAsyncEnumerable<FileInfo> EnumerateExecutablesWithinDirectoryAsync(
        DirectoryInfo directory,
        SearchOption directorySearchOption,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Gets all executable files within a specified directory asynchronously.
    /// </summary>
    /// <param name="directory">The directory in which to search for executable files.</param>
    /// <param name="directorySearchOption">Specifies whether to search all directories or only the top-level directory.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to use to request cancellation.</param>
    /// <returns>A task that returns an array of <see cref="FileInfo"/> objects representing the executable files within the specified directory.</returns>
    Task<FileInfo[]> GetExecutablesWithinDirectoryAsync(
        DirectoryInfo directory,
        SearchOption directorySearchOption,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Locates all executable files within a specified drive asynchronously.
    /// </summary>
    /// <param name="driveInfo">The drive in which to search for executable files.</param>
    /// <param name="directorySearchOption">Specifies whether to search all directories or only the top-level directory.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to use to request cancellation.</param>
    /// <returns>An asynchronous sequence of <see cref="FileInfo"/> objects representing the executable files within the specified drive.</returns>
    /// <exception cref="DriveNotFoundException">Thrown when the specified drive does not exist or is unavailable.</exception>
    IAsyncEnumerable<FileInfo> EnumerateExecutablesWithinDriveAsync(DriveInfo driveInfo,
        SearchOption directorySearchOption, CancellationToken cancellationToken);
    
    /// <summary>
    /// Gets all executable files within a specified drive asynchronously.
    /// </summary>
    /// <param name="driveInfo">The drive in which to search for executable files.</param>
    /// <param name="directorySearchOption">Specifies whether to search all directories or only the top-level directory.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to use to request cancellation.</param>
    /// <returns>A task that returns an array of <see cref="FileInfo"/> objects representing the executable files within the specified drive.</returns>
    Task<FileInfo[]> GetExecutablesWithinDriveAsync(DriveInfo driveInfo,
        SearchOption directorySearchOption, CancellationToken cancellationToken);
}