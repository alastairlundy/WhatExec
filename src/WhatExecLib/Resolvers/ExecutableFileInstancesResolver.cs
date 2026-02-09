/*
    WhatExecLib
    Copyright (c) 2025-2026 Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

namespace WhatExecLib;

/// <summary>
/// Represents a class that provides functionality to locate instances of executable files
/// across multiple drives, directories, and files in a system.
/// </summary>
public class ExecutableFileInstancesResolver : IExecutableFileInstancesResolver
{
    private readonly IExecutableFileDetector _executableFileDetector;

    /// <summary>
    /// Provides functionality for locating instances of executable files across drives, directories, and files.
    /// </summary>
    public ExecutableFileInstancesResolver(IExecutableFileDetector executableDetector)
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
    public FileInfo[] LocateExecutableInstances(string executableName,
        SearchOption directorySearchOption)
    {
        ArgumentException.ThrowIfNullOrEmpty(executableName);

        IEnumerable<DriveInfo> drives = DriveInfo.SafelyEnumerateLogicalDrives();

        IEnumerable<FileInfo> result = drives
            .SelectMany(drive =>
                LocateExecutableInstancesInDrive(drive, executableName, directorySearchOption))
            .AsParallel();

        return result.ToArray();
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
    public FileInfo[] LocateExecutableInstancesInDrive(DriveInfo driveInfo,
        string executableName,
        SearchOption directorySearchOption)
    {
        ArgumentException.ThrowIfNullOrEmpty(executableName);
        
        IEnumerable<FileInfo> results = executableName.GetSearchPatterns()
            .SelectMany(sp => driveInfo.RootDirectory.SafelyEnumerateFiles(sp, directorySearchOption)
                .Where(f => f.Exists
                            && _executableFileDetector.IsFileExecutableAsync(f, CancellationToken.None).Result
                            && f.Name.Equals(executableName)
                ));

        return results.ToArray();
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
    public FileInfo[] LocateExecutableInstancesInDirectory(DirectoryInfo directory,
        string executableName,
        SearchOption directorySearchOption)
    {
        ArgumentException.ThrowIfNullOrEmpty(executableName);
        
        IEnumerable<FileInfo> results = executableName.GetSearchPatterns()
            .SelectMany(sp => directory.SafelyEnumerateFiles(sp, directorySearchOption)
                .Where(f => f.Exists)
                .Where(file => _executableFileDetector.IsFileExecutable(file))
                .Where(file => file.Name.Equals(executableName)));

        return results.ToArray();
    }
}