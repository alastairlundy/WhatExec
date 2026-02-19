/*
    WhatExecLib
    Copyright (c) 2025-2026 Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using WhatExec.Lib.Abstractions.Detectors;

namespace WhatExec.Lib;

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

    public event EventHandler<FileInfo>? ExecutableFileLocated;

    /// <summary>
    /// Locates all instances of the specified executable file across all available drives on the system.
    /// </summary>
    /// <param name="executableName">The name of the executable file to be located.</param>
    /// <param name="directorySearchOption"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>An array of <see cref="FileInfo"/> objects representing the located executable file instances.</returns>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("android")]
    public async Task<FileInfo[]> LocateExecutableInstancesAsync(string executableName,
        SearchOption directorySearchOption, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(executableName);

        IEnumerable<DriveInfo> drives = DriveInfo.SafelyEnumerateLogicalDrives();

        List<FileInfo> output = new();
        
        foreach (DriveInfo drive in drives)
        {
            FileInfo[] driveResults = await LocateExecutableInstancesInDriveAsync(drive, executableName, directorySearchOption, cancellationToken);
           
            output.AddRange(driveResults);
        }

        return output.ToArray();
    }

    /// <summary>
    /// Locates all instances of the specified executable file within a specific drive on the system.
    /// </summary>
    /// <param name="driveInfo">The drive on which to search for the executable file instances.</param>
    /// <param name="executableName">The name of the executable file to be located.</param>
    /// <param name="directorySearchOption"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>An array of <see cref="FileInfo"/> objects representing the located executable file instances within the specified drive.</returns>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("android")]
    public async Task<FileInfo[]> LocateExecutableInstancesInDriveAsync(DriveInfo driveInfo,
        string executableName,
        SearchOption directorySearchOption, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(executableName);

        List<FileInfo> output = new();

        IEnumerable<FileInfo> files = GetSearchPatterns(executableName)
            .SelectMany(sp => driveInfo.RootDirectory.SafelyEnumerateFiles(sp, directorySearchOption))
            .Where(f => f.Exists && f.Name.Equals(executableName));

        foreach (FileInfo file in files)
        {
            try
            {
                bool isExecutable = await _executableFileDetector.IsFileExecutableAsync(file, cancellationToken);

                if (isExecutable)
                {
                    ExecutableFileLocated?.Invoke(this, file);
                    output.Add(file);
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Skip if not authorized.
            }
        }
        
        return output.ToArray();
    }

    /// <summary>
    /// Locates instances of an executable file within the specified directory.
    /// </summary>
    /// <param name="directory">The directory where the search will be conducted.</param>
    /// <param name="executableName">The name of the executable file to search for.</param>
    /// <param name="directorySearchOption"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>An array of <see cref="FileInfo"/> objects representing the located executable files within the directory.</returns>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("android")]
    public async Task<FileInfo[]> LocateExecutableInstancesInDirectoryAsync(DirectoryInfo directory,
        string executableName,
        SearchOption directorySearchOption, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(executableName);

        List<FileInfo> output = new();
        
        IEnumerable<FileInfo> files = GetSearchPatterns(executableName)
            .SelectMany(sp => directory.SafelyEnumerateFiles(sp, directorySearchOption))
            .Where(f => f.Exists && f.Name.Equals(executableName));

        foreach (FileInfo file in files)
        {
            try
            {
                bool isExecutable = await _executableFileDetector.IsFileExecutableAsync(file, cancellationToken);

                if (isExecutable)
                {
                    ExecutableFileLocated?.Invoke(this, file);
                    output.Add(file);
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Skip if not authorized.
            }
        }

        return output.ToArray();
    }

    #region Helper Code

    private static IEnumerable<string> GetSearchPatterns(string executableFileName)
    {
        FileInfo fileInfo = new FileInfo(executableFileName);

        if (Path.HasExtension(executableFileName))
        {
            yield return fileInfo.Extension;
        }

        yield return fileInfo.Name;
    }
    #endregion
}