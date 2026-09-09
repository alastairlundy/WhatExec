/*
    WhatExec.Lib
    Copyright (c) 2025-2026 Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

namespace WhatExec.Lib.Locators;

/// <summary>
/// Defines methods for locating executable file instances across various locations such as drives and directories.
/// </summary>
[Obsolete("Use IExecutableInstancesLocator instead. This interface will be removed in a future version. (D005, D019)")]
public interface IExecutableFileInstancesLocator
{
    /// <summary>
    /// An event that is triggered each time an instance of an executable file was located during the resolution process.
    /// </summary>
    [Obsolete("Events are removed from the new seam. Use enumeration instead. (D008)")]
    event EventHandler<FileInfo> ExecutableFileInstanceLocated;
    
    /// <summary>
    /// Enumerates all instances of the specified executable file across all available drives on the system.
    /// </summary>
    /// <param name="executableName">The name of the executable file to be located.</param>
    /// <param name="directorySearchOption">Specifies whether to search all directories or only the top-level directory.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to use to request cancellation.</param>
    /// <returns>An asynchronous sequence of <see cref="FileInfo"/> objects representing the located executable file instances within the system.</returns>
    [Obsolete("Use IExecutableInstancesLocator.EnumerateExecutableInstancesAcrossDrivesAsync instead. This member will be removed in a future version. (D005)")]
    IAsyncEnumerable<FileInfo> EnumerableExecutableInstancesAsync(string executableName, SearchOption directorySearchOption,
        CancellationToken cancellationToken);
    
    /// <summary>
    /// Locates all instances of the specified executable file across all available drives on the system.
    /// </summary>
    /// <param name="executableName">The name of the executable file to be located.</param>
    /// <param name="directorySearchOption">Specifies whether to search all directories or only the top-level directory.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to use to request cancellation.</param>
    /// <returns>An array of <see cref="FileInfo"/> objects representing the located executable file instances within the system.</returns>
    [Obsolete("Use IExecutableInstancesLocator.EnumerateExecutableInstancesAcrossDrivesAsync instead. This member will be removed in a future version. (D005)")]
    Task<FileInfo[]> GetExecutableInstancesAsync(
        string executableName,
        SearchOption directorySearchOption,
        CancellationToken cancellationToken
    );
    
    /// <summary>
    /// Enumerates instances of the specified executable file within the specified drive.
    /// </summary>
    /// <param name="driveInfo">The drive on which to search for the executable file instances.</param>
    /// <param name="executableName">The name of the executable file to be located.</param>
    /// <param name="directorySearchOption">Specifies whether to search all directories or only the top-level directory.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to use to request cancellation.</param>
    /// <returns>An asynchronous sequence of <see cref="FileInfo"/> objects representing the located executable file instances within the specified drive.</returns>
    [Obsolete("Use IExecutableInstancesLocator.EnumerateExecutableInstancesInDriveAsync instead. This member will be removed in a future version. (D005)")]
    IAsyncEnumerable<FileInfo> EnumerableExecutableInstancesInDriveAsync(DriveInfo driveInfo,
        string executableName, SearchOption directorySearchOption, CancellationToken cancellationToken);

    /// <summary>
    /// Locates all instances of the specified executable file within the specified drive.
    /// </summary>
    /// <param name="driveInfo">The drive on which to search for the executable file instances.</param>
    /// <param name="executableName">The name of the executable file to be located.</param>
    /// <param name="directorySearchOption">Specifies whether to search all directories or only the top-level directory.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to use to request cancellation.</param>
    /// <returns>An array of <see cref="FileInfo"/> objects representing the located executable file instances within the specified drive.</returns>
    [Obsolete("Use IExecutableInstancesLocator.EnumerateExecutableInstancesInDriveAsync instead. This member will be removed in a future version. (D005)")]
    Task<FileInfo[]> GetExecutableInstancesInDriveAsync(
        DriveInfo driveInfo,
        string executableName,
        SearchOption directorySearchOption,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Enumerates instances of the specified executable file within the specified directory.
    /// </summary>
    /// <param name="directory">The directory where the search will be conducted.</param>
    /// <param name="executableName">The name of the executable file to search for.</param>
    /// <param name="directorySearchOption">Specifies whether to search all directories or only the top-level directory.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to use to request cancellation.</param>
    /// <returns>An asynchronous sequence of <see cref="FileInfo"/> objects representing the located executable file instances within the specified directory.</returns>
    [Obsolete("Use IExecutableInstancesLocator.EnumerateExecutableInstancesInDirectoryAsync instead. This member will be removed in a future version. (D005)")]
    IAsyncEnumerable<FileInfo> EnumerableExecutableInstancesInDirectoryAsync(DirectoryInfo directory,
        string executableName,
        SearchOption directorySearchOption,
        CancellationToken cancellationToken
    );
    
    /// <summary>
    /// Locates instances of an executable file within the specified directory.
    /// </summary>
    /// <param name="directory">The directory where the search will be conducted.</param>
    /// <param name="executableName">The name of the executable file to search for.</param>
    /// <param name="directorySearchOption">Specifies whether to search all directories or only the top-level directory.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to use to request cancellation.</param>
    /// <returns>An array of <see cref="FileInfo"/> objects representing the located executable file instances within the specified directory.</returns>
    [Obsolete("Use IExecutableInstancesLocator.EnumerateExecutableInstancesInDirectoryAsync instead. This member will be removed in a future version. (D005)")]
    Task<FileInfo[]> GetExecutableInstancesInDirectoryAsync(
        DirectoryInfo directory,
        string executableName,
        SearchOption directorySearchOption,
        CancellationToken cancellationToken
    );
}
