/*
    WhatExec.Lib
    Copyright (c) 2025-2026 Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

namespace WhatExec.Lib.Locators;

/// <summary>
/// Defines an interface for locating all executable files within directories, drives, or across drives.
/// Split-pair discovery seam for all-executables queries (D019).
/// No events (D008); no PATH knowledge (D002, D011).
/// </summary>
public interface IExecutablesLocator
{
    /// <summary>
    /// Enumerates all executable files within the specified directory asynchronously.
    /// </summary>
    /// <param name="directory">The directory in which to search for executable files.</param>
    /// <param name="search">Specifies whether to search all subdirectories or only the top-level directory.</param>
    /// <param name="ct">The <see cref="CancellationToken"/> to use to request cancellation.</param>
    /// <returns>An asynchronous sequence of <see cref="FileInfo"/> objects representing executable files found.</returns>
    IAsyncEnumerable<FileInfo> EnumerateExecutablesInDirectoryAsync(
        DirectoryInfo directory,
        SearchOption search,
        CancellationToken ct
    );

    /// <summary>
    /// Enumerates all executable files within the specified drive asynchronously.
    /// </summary>
    /// <param name="drive">The drive in which to search for executable files.</param>
    /// <param name="search">Specifies whether to search all subdirectories or only the top-level directory.</param>
    /// <param name="ct">The <see cref="CancellationToken"/> to use to request cancellation.</param>
    /// <returns>An asynchronous sequence of <see cref="FileInfo"/> objects representing executable files found.</returns>
    IAsyncEnumerable<FileInfo> EnumerateExecutablesInDriveAsync(
        DriveInfo drive,
        SearchOption search,
        CancellationToken ct
    );

    /// <summary>
    /// Enumerates all executable files across all available drives asynchronously.
    /// Across-drives sugar over DriveInfo.GetDrives() fan-out (D017).
    /// </summary>
    /// <param name="search">Specifies whether to search all subdirectories or only the top-level directory.</param>
    /// <param name="ct">The <see cref="CancellationToken"/> to use to request cancellation.</param>
    /// <returns>An asynchronous sequence of <see cref="FileInfo"/> objects representing executable files found.</returns>
    IAsyncEnumerable<FileInfo> EnumerateExecutablesAcrossDrivesAsync(
        SearchOption search,
        CancellationToken ct
    );

    // ── Obsolete legacy members (D005) ────────────────────────────────────

    /// <summary>
    /// Gets all executable files within the specified directory asynchronously.
    /// </summary>
    [Obsolete("Use EnumerateExecutablesInDirectoryAsync instead. (D005)")]
    Task<FileInfo[]> GetExecutablesInDirectoryAsync(
        DirectoryInfo directory,
        SearchOption directorySearchOption,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Gets all executable files within the specified drive asynchronously.
    /// </summary>
    [Obsolete("Use EnumerateExecutablesInDriveAsync instead. (D005)")]
    Task<FileInfo[]> GetExecutablesInDriveAsync(
        DriveInfo driveInfo,
        SearchOption directorySearchOption,
        CancellationToken cancellationToken
    );
}
