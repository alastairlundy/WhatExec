/*
    WhatExecLib
    Copyright (c) 2025 Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using DotExtensions.IO.Directories;

namespace WhatExecLib.Locators;

/// <summary>
/// Represents a class that provides functionality to locate instances of executable files
/// across multiple drives, directories, and files in a system.
/// </summary>
public class ExecutableFileInstancesLocator : IExecutableFileInstancesLocator
{
    private readonly IExecutableFileDetector _executableFileDetector;

    /// <summary>
    /// Provides functionality for locating instances of executable files across drives, directories, and files.
    /// </summary>
    public ExecutableFileInstancesLocator(IExecutableFileDetector executableDetector)
    {
        _executableFileDetector = executableDetector;
    }

    /// <summary>
    /// Locates all instances of the specified executable file across all available drives on the system.
    /// </summary>
    /// <param name="executableName">The name of the executable file to be located.</param>
    /// <param name="directorySearchOption"></param>
    /// <returns>An array of <see cref="FileInfo"/> objects representing the located executable file instances.</returns>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("android")]
    public IEnumerable<FileInfo> LocateExecutableInstances(
        string executableName,
        SearchOption directorySearchOption
    )
    {
        ArgumentException.ThrowIfNullOrEmpty(executableName);

        IEnumerable<DriveInfo> drives = DriveDetector.EnumerateDrives();

        IEnumerable<FileInfo> result = drives
            .SelectMany(drive =>
                LocateExecutableInstancesInDrive(drive, executableName, directorySearchOption)
            )
            .AsParallel();

        return result;
    }

    /// <summary>
    /// Locates all instances of the specified executable file within a specific drive on the system.
    /// </summary>
    /// <param name="driveInfo">The drive on which to search for the executable file instances.</param>
    /// <param name="executableName">The name of the executable file to be located.</param>
    /// <param name="directorySearchOption"></param>
    /// <returns>An array of <see cref="FileInfo"/> objects representing the located executable file instances within the specified drive.</returns>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("android")]
    public IEnumerable<FileInfo> LocateExecutableInstancesInDrive(
        DriveInfo driveInfo,
        string executableName,
        SearchOption directorySearchOption
    )
    {
        ArgumentException.ThrowIfNullOrEmpty(executableName);

        IEnumerable<string> searchPatterns = executableName.GetSearchPatterns();

        IEnumerable<FileInfo> results = searchPatterns
            .SelectMany(sp =>
                driveInfo.RootDirectory.SafelyEnumerateFiles(sp, directorySearchOption)
            )
            .PrioritizeLocations()
            .Where(f =>
                f is not null
                && f.Exists
                && _executableFileDetector.IsFileExecutable(f)
                && f.Name.Equals(executableName)
            );

        return results;
    }

    /// <summary>
    /// Locates instances of an executable file within the specified directory.
    /// </summary>
    /// <param name="directory">The directory where the search will be conducted.</param>
    /// <param name="executableName">The name of the executable file to search for.</param>
    /// <param name="directorySearchOption"></param>
    /// <returns>An array of <see cref="FileInfo"/> objects representing the located executable files within the directory.</returns>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("android")]
    public IEnumerable<FileInfo> LocateExecutableInstancesInDirectory(
        DirectoryInfo directory,
        string executableName,
        SearchOption directorySearchOption
    )
    {
        ArgumentException.ThrowIfNullOrEmpty(executableName);

        IEnumerable<string> searchPatterns = executableName.GetSearchPatterns();

        IEnumerable<FileInfo> results = searchPatterns
            .SelectMany(sp => directory.SafelyEnumerateFiles(sp, directorySearchOption))
            .PrioritizeLocations()
            .Where(f => f.Exists)
            .Where(file => _executableFileDetector.IsFileExecutable(file))
            .Where(file => file.Name.Equals(executableName));

        return results;
    }
}
