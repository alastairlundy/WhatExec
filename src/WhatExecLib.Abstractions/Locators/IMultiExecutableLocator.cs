/*
    WhatExecLib
    Copyright (c) 2025 Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

namespace WhatExecLib.Abstractions.Locators;

/// <summary>
/// Defines an interface for locating all executable files within a specified directory or drive.
/// </summary>
public interface IMultiExecutableLocator
{
    /// <summary>
    /// Locates all executable files within a specified directory asynchronously.
    /// Filters the files to include only those that are recognized as executable based on the implementation of the provided executable file detector.
    /// </summary>
    /// <param name="directory">The directory in which to search for executable files.</param>
    /// <param name="directorySearchOption"></param>
    /// <returns>A collection of <see cref="FileInfo"/> objects representing the executable files within the specified directory.</returns>
    /// <exception cref="DirectoryNotFoundException">Thrown when the specified directory does not exist.</exception>
    IEnumerable<FileInfo> LocateAllExecutablesWithinDirectory(
        DirectoryInfo directory,
        SearchOption directorySearchOption
    );

    /// <summary>
    /// Locates all executable files within a specified drive asynchronously.
    /// Filters the files to include only those recognized as executables based on the provided executable file detector implementation.
    /// </summary>
    /// <param name="driveInfo">The drive in which to search for executable files.</param>
    /// <param name="searchOption"></param>
    /// <returns>A collection of <see cref="FileInfo"/> objects representing the executable files within the specified drive.</returns>
    /// <exception cref="DriveNotFoundException">Thrown when the specified drive does not exist or is unavailable.</exception>
    IEnumerable<FileInfo> LocateAllExecutablesWithinDrive(
        DriveInfo driveInfo,
        SearchOption searchOption
    );
}
