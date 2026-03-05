/*
    WhatExecLib
    Copyright (c) 2025-2026 Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Runtime.CompilerServices;
using WhatExec.Lib.Abstractions.Detectors;

namespace WhatExec.Lib;

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

    /// <inheritdoc/>
    public event EventHandler<FileInfo>? ExecutableFileLocated;
    
    /// <summary>
    ///
    /// </summary>
    /// <param name="directory"></param>
    /// <param name="directorySearchOption"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="DirectoryNotFoundException"></exception>
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
    public async IAsyncEnumerable<FileInfo> EnumerateExecutablesWithinDirectoryAsync(DirectoryInfo directory,
        SearchOption directorySearchOption, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if(!directory.Exists)
            throw new DirectoryNotFoundException("The specified directory does not exist");
        
        IEnumerable<FileInfo> files = directory.SafelyEnumerateFiles("*",
                directorySearchOption)
            .Where(file => file.Exists);

        foreach (FileInfo file in files)
        {
            bool isExecutable;
            
            try
            {
                isExecutable = await _executableFileDetector.IsFileExecutableAsync(file, cancellationToken);
            }
            catch (UnauthorizedAccessException)
            {
                isExecutable = file.HasExecutePermission();
            }

            if (isExecutable)
            {
                ExecutableFileLocated?.Invoke(this, file);
                yield return file;
            }
        }
    }

    /// <summary>
    /// Locates all executable files within the specified directory and its subdirectories.
    /// </summary>
    /// <param name="directory">The directory to search for executables.</param>
    /// <param name="directorySearchOption"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>An array of <see cref="FileInfo"/> objects representing the executable files found.</returns>
    /// <exception cref="DirectoryNotFoundException">Thrown when the specified directory does not exist.</exception>
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
    public async Task<FileInfo[]> GetExecutablesWithinDirectoryAsync(DirectoryInfo directory, SearchOption directorySearchOption,
        CancellationToken cancellationToken)
        => await EnumerateExecutablesWithinDirectoryAsync(directory, directorySearchOption, cancellationToken)
            .ToArrayAsync(cancellationToken: cancellationToken);

    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="driveInfo"></param>
    /// <param name="directorySearchOption"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
    public async IAsyncEnumerable<FileInfo> EnumerateExecutablesWithinDriveAsync(DriveInfo driveInfo,
        SearchOption directorySearchOption, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if(!driveInfo.IsReady)
            throw new ArgumentException("The specified drive is not ready");
        
        IEnumerable<FileInfo> files = driveInfo.RootDirectory.SafelyEnumerateFiles("*",
                directorySearchOption)
            .Where(file => file.Exists);

        foreach (FileInfo file in files)
        {
            bool isExecutable;
            
            try
            {
                isExecutable = await _executableFileDetector.IsFileExecutableAsync(file, cancellationToken);
            }
            catch (UnauthorizedAccessException)
            {
                isExecutable =  file.HasExecutePermission();
            }

            if (isExecutable)
            {
                ExecutableFileLocated?.Invoke(this, file);
                yield return file;
            }
        }
    }

    /// <summary>
    /// Identifies all executable files within the specified drive by recursively searching through all directories.
    /// </summary>
    /// <param name="driveInfo">The drive to search within for executable files.</param>
    /// <param name="directorySearchOption"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>An array of FileInfo objects representing executable files found within the drive.</returns>
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
    public async Task<FileInfo[]> GetExecutablesWithinDriveAsync(DriveInfo driveInfo, SearchOption directorySearchOption,
        CancellationToken cancellationToken)
        => await EnumerateExecutablesWithinDriveAsync(driveInfo, directorySearchOption, cancellationToken)
            .ToArrayAsync(cancellationToken);
}