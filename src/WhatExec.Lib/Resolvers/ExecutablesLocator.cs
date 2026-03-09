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
/// Represents a locator that identifies all executable files within specified directories or drives.
/// </summary>
public class ExecutablesLocator : IExecutablesLocator
{
    private readonly IExecutableFileDetector _executableFileDetector;

    /// <summary>
    /// Represents a locator for identifying all executable files within specified directories or drives.
    /// </summary>
    /// <param name="executableFileDetector">The executable file detector to use.</param>
    public ExecutablesLocator(IExecutableFileDetector executableFileDetector)
    {
        _executableFileDetector = executableFileDetector;
    }

    /// <inheritdoc/>
    public event EventHandler<FileInfo>? ExecutableFileLocated;

    /// <summary>
    /// Asynchronously enumerates executable files within a specified directory.
    /// </summary>
    /// <param name="directory">The directory to enumerate.</param>
    /// <param name="directorySearchOption">A value that specifies whether to search subdirectories or not.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>An asynchronous enumerable sequence of <see cref="FileInfo"/> objects representing executable files.</returns>
    /// <exception cref="DirectoryNotFoundException">Thrown if the specified directory does not exist.</exception>
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
    /// <param name="directorySearchOption">A value that specifies whether to search subdirectories or not.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
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
    /// Asynchronously enumerates executable files within a specified drive.
    /// </summary>
    /// <param name="driveInfo">The drive information.</param>
    /// <param name="directorySearchOption">Specifies whether to search all subdirectories or only the specified directory.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>An asynchronous enumerable sequence of <see cref="FileInfo"/> objects representing executable files within the drive.</returns>
    /// <exception cref="ArgumentException">Thrown when the specified drive is not ready.</exception>
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
    /// <param name="directorySearchOption">Specifies whether to search all subdirectories or only the specified directory.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>An array of FileInfo objects representing executable files found within the drive.</returns>
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
    public async Task<FileInfo[]> GetExecutablesWithinDriveAsync(DriveInfo driveInfo, SearchOption directorySearchOption,
        CancellationToken cancellationToken)
        => await EnumerateExecutablesWithinDriveAsync(driveInfo, directorySearchOption, cancellationToken)
            .ToArrayAsync(cancellationToken);
}