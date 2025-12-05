/*
    WhatExecLib
    Copyright (c) 2025 Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using AlastairLundy.WhatExecLib.Extensions;

namespace AlastairLundy.WhatExecLib.Locators;

/// <summary>
/// Represents a locator that identifies all executable files within specified directories or drives.
/// Implements the <see cref="IMultiExecutableLocator"/> interface to provide functionality for locating executables.
/// </summary>
public class MultiExecutableLocator : IMultiExecutableLocator
{
    private readonly IExecutableFileDetector _executableFileDetector;

    /// <summary>
    /// Represents a locator for identifying all executable files within specified directories or drives.
    /// </summary>
    public MultiExecutableLocator(IExecutableFileDetector executableFileDetector)
    {
        _executableFileDetector = executableFileDetector;
    }

    /// <summary>
    /// Locates all executable files within the specified directory and its subdirectories.
    /// </summary>
    /// <param name="directory">The directory to search for executables.</param>
    /// <param name="directorySearchOption"></param>
    /// <returns>An array of <see cref="FileInfo"/> objects representing the executable files found.</returns>
    /// <exception cref="DirectoryNotFoundException">Thrown when the specified directory does not exist.</exception>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("android")]
    public IEnumerable<FileInfo> LocateAllExecutablesWithinDirectory(
        DirectoryInfo directory,
        SearchOption directorySearchOption
    )
    {
        IEnumerable<FileInfo> results = directory
            .EnumerateFiles("*", directorySearchOption)
            .Where(file => _executableFileDetector.IsFileExecutable(file));

        return results;
    }

    /// <summary>
    /// Identifies all executable files within the specified drive by recursively searching through all directories.
    /// </summary>
    /// <param name="driveInfo">The drive to search within for executable files.</param>
    /// <param name="directorySearchOption"></param>
    /// <returns>An array of FileInfo objects representing executable files found within the drive.</returns>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("android")]
    public IEnumerable<FileInfo> LocateAllExecutablesWithinDrive(
        DriveInfo driveInfo,
        SearchOption directorySearchOption
    )
    {
        IEnumerable<FileInfo> results = driveInfo
            .RootDirectory.EnumerateDirectories("*", directorySearchOption)
            .PrioritizeLocations()
            .SelectMany(dir => LocateAllExecutablesWithinDirectory(dir, directorySearchOption))
            .AsParallel();

        return results;
    }
}
