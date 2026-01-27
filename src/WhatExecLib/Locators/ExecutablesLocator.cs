/*
    WhatExecLib
    Copyright (c) 2025-2026 Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

namespace WhatExecLib.Locators;

/// <summary>
/// Represents a locator that identifies all executable files within specified directories or drives.
/// Implements the <see cref="IExecutablesLocator"/> interface to provide functionality for locating executables.
/// </summary>
public class ExecutablesLocator : IExecutablesLocator
{
    private readonly IExecutableFileDetector _executableFileDetector;

    /// <summary>
    /// Represents a locator for identifying all executable files within specified directories or drives.
    /// </summary>
    public ExecutablesLocator(IExecutableFileDetector executableFileDetector)
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
    public FileInfo[] LocateAllExecutablesWithinDirectory(DirectoryInfo directory,
        SearchOption directorySearchOption)
    {
        if(!directory.Exists)
            throw new DirectoryNotFoundException("The specified directory does not exist");
        
        return directory
            .SafelyEnumerateFiles("*", directorySearchOption)
            .PrioritizeLocations()
            .Where(file => file.Exists && _executableFileDetector.IsFileExecutable(file))
            .ToArray();
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
    public FileInfo[] LocateAllExecutablesWithinDrive(
        DriveInfo driveInfo,
        SearchOption directorySearchOption)
    {
        if(!driveInfo.IsReady)
            throw new ArgumentException("The specified drive is not ready");
            
        return driveInfo
            .RootDirectory.SafelyEnumerateFiles("*", directorySearchOption)
            .PrioritizeLocations()
            .Where(file => file.Exists && _executableFileDetector.IsFileExecutable(file))
            .ToArray();
    }
}
