/*
    WhatExec.Lib
    Copyright (c) 2025-2026 Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

namespace WhatExec.Lib.Locators;

/// <summary>
/// Defines an interface for locating named executable file instances within directories, drives, or across drives.
/// Split-pair discovery seam for named-instances queries (D019).
/// No events (D008); no PATH knowledge (D002, D011).
/// </summary>
public interface IExecutableInstancesLocator
{
    /// <summary>
    /// Enumerates instances of the specified executable file within the specified directory asynchronously.
    /// </summary>
    /// <param name="directory">The directory in which to search for the executable file instances.</param>
    /// <param name="executableName">The name of the executable file to locate.</param>
    /// <param name="search">Specifies whether to search all subdirectories or only the top-level directory.</param>
    /// <param name="ct">The <see cref="CancellationToken"/> to use to request cancellation.</param>
    /// <returns>An asynchronous sequence of <see cref="FileInfo"/> objects representing the located executable file instances.</returns>
    IAsyncEnumerable<FileInfo> EnumerateExecutableInstancesInDirectoryAsync(
        DirectoryInfo directory,
        string executableName,
        SearchOption search,
        CancellationToken ct
    );

    /// <summary>
    /// Enumerates instances of the specified executable file within the specified drive asynchronously.
    /// </summary>
    /// <param name="drive">The drive in which to search for the executable file instances.</param>
    /// <param name="executableName">The name of the executable file to locate.</param>
    /// <param name="search">Specifies whether to search all subdirectories or only the top-level directory.</param>
    /// <param name="ct">The <see cref="CancellationToken"/> to use to request cancellation.</param>
    /// <returns>An asynchronous sequence of <see cref="FileInfo"/> objects representing the located executable file instances.</returns>
    IAsyncEnumerable<FileInfo> EnumerateExecutableInstancesInDriveAsync(
        DriveInfo drive,
        string executableName,
        SearchOption search,
        CancellationToken ct
    );

    /// <summary>
    /// Enumerates instances of the specified executable file across all available drives asynchronously.
    /// Across-drives sugar over DriveInfo.GetDrives() fan-out (D017).
    /// </summary>
    /// <param name="executableName">The name of the executable file to locate.</param>
    /// <param name="search">Specifies whether to search all subdirectories or only the top-level directory.</param>
    /// <param name="ct">The <see cref="CancellationToken"/> to use to request cancellation.</param>
    /// <returns>An asynchronous sequence of <see cref="FileInfo"/> objects representing the located executable file instances.</returns>
    IAsyncEnumerable<FileInfo> EnumerateExecutableInstancesAcrossDrivesAsync(
        string executableName,
        SearchOption search,
        CancellationToken ct
    );
}
