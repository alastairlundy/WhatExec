/*
    WhatExecLib
    Copyright (c) 2025-2026 Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

namespace WhatExecLib.Abstractions.Locators;

/// <summary>
/// Defines methods for locating executable file instances across various locations such as drives and directories.
/// </summary>
public interface IExecutableFileInstancesLocator
{
    /// <summary>
    /// Locates all instances of the specified executable file across all available drives on the system.
    /// </summary>
    /// <param name="executableName">The name of the executable file to be located.</param>
    /// <param name="directorySearchOption"></param>
    /// <returns>An array of FileInfo objects representing the located executable file instances.</returns>
    FileInfo[] LocateExecutableInstances(
        string executableName,
        SearchOption directorySearchOption
    );

    /// <summary>
    /// Locates all instances of the specified executable file within a specific drive on the system.
    /// </summary>
    /// <param name="driveInfo">The drive on which to search for the executable file instances.</param>
    /// <param name="executableName">The name of the executable file to be located.</param>
    /// <param name="directorySearchOption"></param>
    /// <returns>An array of FileInfo objects representing the located executable file instances within the specified drive.</returns>
    FileInfo[] LocateExecutableInstancesInDrive(
        DriveInfo driveInfo,
        string executableName,
        SearchOption directorySearchOption
    );

    /// <summary>
    /// Locates instances of an executable file within the specified directory.
    /// </summary>
    /// <param name="directory">The directory where the search will be conducted.</param>
    /// <param name="executableName">The name of the executable file to search for.</param>
    /// <param name="directorySearchOption"></param>
    /// <returns>An array of FileInfo objects representing the located executable files within the directory.</returns>
    FileInfo[] LocateExecutableInstancesInDirectory(
        DirectoryInfo directory,
        string executableName,
        SearchOption directorySearchOption
    );
}
