/*
    XpWhereLib
    Copyright (c) 2025 Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading.Tasks;

using XpWhichLib.Abstractions;

namespace XpWhichLib;

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
    public IEnumerable<FileInfo> LocateExecutableInstances(string executableName, SearchOption directorySearchOption)
    {
        IEnumerable<DriveInfo> drives = DriveInfo.GetDrives()
            .Where(x => x.IsReady);

        ParallelQuery<FileInfo> result = drives.AsParallel()
            .SelectMany(drive =>
                LocateExecutableInstancesWithinDrive(drive, executableName, SearchOption.AllDirectories));
    
        return result;
    }

    /// <summary>
    /// Locates all instances of the specified executable file within a specific drive on the system.
    /// </summary>
    /// <param name="driveInfo">The drive on which to search for the executable file instances.</param>
    /// <param name="executableName">The name of the executable file to be located.</param>
    /// <param name="directorySearchOption"></param>
    /// <returns>An array of <see cref="FileInfo"/> objects representing the located executable file instances within the specified drive.</returns>
    public IEnumerable<FileInfo> LocateExecutableInstancesWithinDrive(DriveInfo driveInfo,
        string executableName, SearchOption directorySearchOption)
    {
        ParallelQuery<FileInfo> results = driveInfo.RootDirectory
            .EnumerateDirectories("*", directorySearchOption)
            .AsParallel()
            .SelectMany(dir => LocateExecutableInstancesWithinDirectory(dir, executableName,  directorySearchOption));

        return results;
    }

    /// <summary>
    /// Locates instances of an executable file within the specified directory.
    /// </summary>
    /// <param name="directory">The directory where the search will be conducted.</param>
    /// <param name="executableName">The name of the executable file to search for.</param>
    /// <param name="directorySearchOption"></param>
    /// <returns>An array of <see cref="FileInfo"/> objects representing the located executable files within the directory.</returns>
    public IEnumerable<FileInfo> LocateExecutableInstancesWithinDirectory(DirectoryInfo directory,
        string executableName, SearchOption directorySearchOption)
    {
        ParallelQuery<FileInfo> results = directory
            .EnumerateFiles("*", directorySearchOption)
            .AsParallel()
            .Where(file => _executableFileDetector.IsFileExecutable(file))
            .Where(file => file.Exists && (file.Name.Equals(executableName) || file.FullName.Equals(executableName)));

        return results;
    }
}