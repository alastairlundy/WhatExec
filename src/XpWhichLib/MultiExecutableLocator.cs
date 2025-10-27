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
    /// <returns>An array of <see cref="FileInfo"/> objects representing the executable files found.</returns>
    /// <exception cref="DirectoryNotFoundException">Thrown when the specified directory does not exist.</exception>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    public FileInfo[] LocateAllExecutablesWithinDirectory(DirectoryInfo directory)
    {
        ConcurrentBag<FileInfo> results = new();
        
        IEnumerable<DirectoryInfo> directories = new DirectoryInfo(directory.FullName)
            .EnumerateDirectories("*",  SearchOption.AllDirectories);

        Parallel.ForEach(directories, dir =>
        {
            IEnumerable<FileInfo> files = new DirectoryInfo(dir.FullName)
                .EnumerateFiles("*", SearchOption.AllDirectories)
                .Where(file => _executableFileDetector.IsFileExecutable(file));
            
            foreach (FileInfo file in files)
            {
                results.Add(file);
            }
        });
        
        return results.ToArray();
    }

    /// <summary>
    /// Identifies all executable files within the specified drive by recursively searching through all directories.
    /// </summary>
    /// <param name="driveInfo">The drive to search within for executable files.</param>
    /// <returns>An array of FileInfo objects representing executable files found within the drive.</returns>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    public FileInfo[] LocateAllExecutablesWithinDrive(DriveInfo driveInfo)
    {
        ConcurrentBag<FileInfo> results = new();
        
        IEnumerable<DirectoryInfo> directories = new DirectoryInfo(driveInfo.RootDirectory.FullName)
            .EnumerateDirectories("*",  SearchOption.AllDirectories);

        Parallel.ForEach(directories, directory =>
        {
            FileInfo[] files = LocateAllExecutablesWithinDirectory(directory);

            foreach (FileInfo file in files)
            {
                results.Add(file);
            }
        });
        
        return results.ToArray();
    }
}