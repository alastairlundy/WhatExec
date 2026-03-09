/*
    WhatExec.Lib
    Copyright (c) 2025-2026 Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Runtime.CompilerServices;
using WhatExec.Lib.Abstractions.Detectors;

namespace WhatExec.Lib;

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

    public event EventHandler<FileInfo>? ExecutableFileInstanceLocated;

    /// <summary>
    /// Asynchronously enumerates resolved <see cref="FileInfo"/> objects representing executable instances found across logical drives.
    /// </summary>
    /// <param name="executableName">The name of the executable to search for.</param>
    /// <param name="directorySearchOption">Specifies whether to search subdirectories or only the current directory.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <returns>An asynchronous sequence of <see cref="FileInfo"/> objects representing instances of an executable file.</returns>
    public async IAsyncEnumerable<FileInfo> EnumerableExecutableInstancesAsync(string executableName,
        SearchOption directorySearchOption,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(executableName);

        IEnumerable<DriveInfo> drives = DriveInfo.SafelyEnumerateLogicalDrives();
        
        foreach (DriveInfo drive in drives)
        {
            IAsyncEnumerable<FileInfo> driveResults = EnumerableExecutableInstancesInDriveAsync(drive, executableName, directorySearchOption, cancellationToken);

            await foreach (FileInfo driveResult in driveResults.ConfigureAwait(false))
            {
                yield return driveResult;
            }
        }
    }

    /// <summary>
    /// Locates all instances of the specified executable file across all available drives on the system.
    /// </summary>
    /// <param name="executableName">The name of the executable file to be located.</param>
    /// <param name="directorySearchOption">Specifies whether to search subdirectories or only the current directory.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>An array of <see cref="FileInfo"/> objects representing the located executable file instances.</returns>
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
    public async Task<FileInfo[]> GetExecutableInstancesAsync(string executableName,
        SearchOption directorySearchOption, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(executableName);

        IEnumerable<DriveInfo> drives = DriveInfo.SafelyEnumerateLogicalDrives();

        List<FileInfo> output = new();
        
        foreach (DriveInfo drive in drives)
        {
            FileInfo[] driveResults = await GetExecutableInstancesInDriveAsync(drive, executableName, directorySearchOption, cancellationToken).ConfigureAwait(false);
           
            output.AddRange(driveResults);
        }

        return output.ToArray();
    }

    /// <summary>
    /// Enumerates resolved instances of a specified executable name from a specified drive.
    /// </summary>
    /// <param name="driveInfo">The drive information to search.</param>
    /// <param name="executableName">The name of the executable file to find.</param>
    /// <param name="directorySearchOption">Specifies whether to search subdirectories or only the current directory.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>An asynchronous sequence of <see cref="FileInfo"/> objects representing executable files that match the specified criteria.</returns>
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
    public async IAsyncEnumerable<FileInfo> EnumerableExecutableInstancesInDriveAsync(DriveInfo driveInfo,
        string executableName,
        SearchOption directorySearchOption, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(executableName);
        
        IEnumerable<FileInfo> files = EnumerateSearchPatterns(executableName)
            .SelectMany(sp => driveInfo.RootDirectory.SafelyEnumerateFiles(sp, directorySearchOption))
            .Where(f => f.Exists && f.Name.Equals(executableName));

        foreach (FileInfo file in files)
        {
            bool validExecutable = false;
            try
            {
                bool isExecutable = await _executableFileDetector.IsFileExecutableAsync(file, cancellationToken)
                    .ConfigureAwait(false);

                if (isExecutable)
                {
                    ExecutableFileInstanceLocated?.Invoke(this, file);
                    validExecutable = true;
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Skip if not authorized.
                validExecutable = false;
            }
            
            if(validExecutable)
                yield return file;
        }
    }

    /// <summary>
    /// Locates all instances of the specified executable file within a specific drive on the system.
    /// </summary>
    /// <param name="driveInfo">The drive on which to search for the executable file instances.</param>
    /// <param name="executableName">The name of the executable file to be located.</param>
    /// <param name="directorySearchOption">Specifies whether to search subdirectories or only the current directory.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>An array of <see cref="FileInfo"/> objects representing the located executable file instances within the specified drive.</returns>
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
    public async Task<FileInfo[]> GetExecutableInstancesInDriveAsync(DriveInfo driveInfo,
        string executableName,
        SearchOption directorySearchOption, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(executableName);

        List<FileInfo> output = new();

        IEnumerable<FileInfo> files = EnumerateSearchPatterns(executableName)
            .SelectMany(sp => driveInfo.RootDirectory.SafelyEnumerateFiles(sp, directorySearchOption))
            .Where(f => f.Exists && f.Name.Equals(executableName));

        foreach (FileInfo file in files)
        {
            try
            {
                bool isExecutable = await _executableFileDetector.IsFileExecutableAsync(file, cancellationToken)
                    .ConfigureAwait(false);

                if (isExecutable)
                {
                    ExecutableFileInstanceLocated?.Invoke(this, file);
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
    /// Asynchronously enumerates instances of <see cref="FileInfo"/> objects representing executable files within a specified directory that match the given executable name.
    /// </summary>
    /// <param name="directory">The directory to search for executable files.</param>
    /// <param name="executableName">The name of the executable file to locate.</param>
    /// <param name="directorySearchOption">Specifies whether to search subdirectories or only the current directory.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to abort the operation.</param>
    /// <returns>An asynchronous sequence of <see cref="FileInfo"/> objects representing executable files that match the specified criteria.</returns>
    public async IAsyncEnumerable<FileInfo> EnumerableExecutableInstancesInDirectoryAsync(DirectoryInfo directory,
        string executableName,
        SearchOption directorySearchOption, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(executableName);
        
        IEnumerable<FileInfo> files = EnumerateSearchPatterns(executableName)
            .SelectMany(sp => directory.SafelyEnumerateFiles(sp, directorySearchOption))
            .Where(f => f.Exists && f.Name.Equals(executableName));

        foreach (FileInfo file in files)
        {
            bool validExecutable = false;
            try
            {
                bool isExecutable = await _executableFileDetector.IsFileExecutableAsync(file, cancellationToken)
                    .ConfigureAwait(false);

                if (isExecutable)
                {
                    ExecutableFileInstanceLocated?.Invoke(this, file);
                    validExecutable = true;
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Skip if not authorized.
                validExecutable = false;
            }
            
            if(validExecutable)
                yield return file;
        }
    }

    /// <summary>
    /// Locates instances of an executable file within the specified directory.
    /// </summary>
    /// <param name="directory">The directory where the search will be conducted.</param>
    /// <param name="executableName">The name of the executable file to search for.</param>
    /// <param name="directorySearchOption">Specifies whether to search subdirectories or only the current directory.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to abort the operation.</param>
    /// <returns>An array of <see cref="FileInfo"/> objects representing the located executable files within the directory.</returns>
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
    public async Task<FileInfo[]> GetExecutableInstancesInDirectoryAsync(DirectoryInfo directory,
        string executableName,
        SearchOption directorySearchOption, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(executableName);

        List<FileInfo> output = new();
        
        IEnumerable<FileInfo> files = EnumerateSearchPatterns(executableName)
            .SelectMany(sp => directory.SafelyEnumerateFiles(sp, directorySearchOption))
            .Where(f => f.Exists && f.Name.Equals(executableName));

        foreach (FileInfo file in files)
        {
            try
            {
                bool isExecutable = await _executableFileDetector.IsFileExecutableAsync(file, cancellationToken)
                    .ConfigureAwait(false);

                if (isExecutable)
                {
                    ExecutableFileInstanceLocated?.Invoke(this, file);
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

    private static IEnumerable<string> EnumerateSearchPatterns(string executableFileName)
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