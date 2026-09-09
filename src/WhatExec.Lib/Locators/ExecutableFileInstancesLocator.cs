/*
    WhatExec.Lib
    Copyright (c) 2025-2026 Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.IO.Abstractions;

namespace WhatExec.Lib.Locators;

/// <summary>
/// Locates named executable file instances within directories, drives, or across drives.
/// Split-pair discovery seam for named-instances queries (D019).
/// Implements both <see cref="IExecutableInstancesLocator"/> (new) and the obsolete
/// <see cref="IExecutableFileInstancesLocator"/> for backward compatibility (D005).
/// One shared traversal core from <see cref="ExecutablesLocator"/> serves all overloads (D016).
/// Filesystem access goes through System.IO.Abstractions seam (D009).
/// Fault rules: IgnoreInaccessible, consistent casing, fixed patterns, no hot tasks (D010).
/// No PATH knowledge (D002, D011); no events on new seam (D008).
/// </summary>
public class ExecutableFileInstancesLocator : IExecutableInstancesLocator, IExecutableFileInstancesLocator
{
    private readonly ExecutablesLocator _sharedCore;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExecutableFileInstancesLocator"/> class.
    /// </summary>
    /// <param name="detector">The executable file detector used to verify executability per file.</param>
    /// <param name="fileSystem">The filesystem abstraction for all file and directory access.</param>
    public ExecutableFileInstancesLocator(IExecutableFileDetector detector, IFileSystem fileSystem)
    {
        _sharedCore = new ExecutablesLocator(detector, fileSystem);
    }

    // ── IExecutableInstancesLocator (new seam) ────────────────────────────

    /// <inheritdoc/>
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
    public async IAsyncEnumerable<FileInfo> EnumerateExecutableInstancesInDirectoryAsync(
        DirectoryInfo directory,
        string executableName,
        SearchOption search,
        [EnumeratorCancellation] CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executableName);

        await foreach (FileInfo file in _sharedCore.TraversalCoreAsync(directory.FullName, search, executableName, ct)
                           .ConfigureAwait(false))
        {
            yield return file;
        }
    }

    /// <inheritdoc/>
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
    public async IAsyncEnumerable<FileInfo> EnumerateExecutableInstancesInDriveAsync(
        DriveInfo drive,
        string executableName,
        SearchOption search,
        CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executableName);

        if (!drive.IsReady)
            throw new ArgumentException(
                string.Format(Resources.Exceptions_Drives_DriveNotReady, drive.Name),
                nameof(drive));

        await foreach (FileInfo file in _sharedCore.TraversalCoreAsync(drive.RootDirectory.FullName, search, executableName, ct)
                           .ConfigureAwait(false))
        {
            yield return file;
        }
    }

    /// <inheritdoc/>
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
    public async IAsyncEnumerable<FileInfo> EnumerateExecutableInstancesAcrossDrivesAsync(
        string executableName,
        SearchOption search,
        CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executableName);

        DriveInfo[] drives;
        try
        {
            drives = DriveInfo.GetDrives();
        }
        catch (Exception)
        {
            yield break;
        }

        foreach (DriveInfo drive in drives)
        {
            if (!drive.IsReady)
                continue;

            await foreach (FileInfo file in _sharedCore.TraversalCoreAsync(drive.RootDirectory.FullName, search, executableName, ct)
                               .ConfigureAwait(false))
            {
                yield return file;
            }
        }
    }

    // ── IExecutableFileInstancesLocator (obsolete, D005/D008) ─────────────

    /// <inheritdoc/>
    [Obsolete("Events are removed from the new seam. Use enumeration instead. (D008)")]
    public event EventHandler<FileInfo>? ExecutableFileInstanceLocated;

    /// <inheritdoc/>
    [Obsolete("Use EnumerateExecutableInstancesAcrossDrivesAsync instead. (D005)")]
    public async IAsyncEnumerable<FileInfo> EnumerableExecutableInstancesAsync(
        string executableName,
        SearchOption directorySearchOption,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (FileInfo file in EnumerateExecutableInstancesAcrossDrivesAsync(
                           executableName, directorySearchOption, cancellationToken)
                           .ConfigureAwait(false))
        {
            yield return file;
        }
    }

    /// <inheritdoc/>
    [Obsolete("Use EnumerateExecutableInstancesAcrossDrivesAsync instead. (D005)")]
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
    public async Task<FileInfo[]> GetExecutableInstancesAsync(
        string executableName,
        SearchOption directorySearchOption,
        CancellationToken cancellationToken)
        => await EnumerateExecutableInstancesAcrossDrivesAsync(
               executableName, directorySearchOption, cancellationToken)
               .ToArrayAsync(cancellationToken: cancellationToken)
               .ConfigureAwait(false);

    /// <inheritdoc/>
    [Obsolete("Use EnumerateExecutableInstancesInDriveAsync instead. (D005)")]
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
    public async IAsyncEnumerable<FileInfo> EnumerableExecutableInstancesInDriveAsync(
        DriveInfo driveInfo,
        string executableName,
        SearchOption directorySearchOption,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (FileInfo file in EnumerateExecutableInstancesInDriveAsync(
                           driveInfo, executableName, directorySearchOption, cancellationToken)
                           .ConfigureAwait(false))
        {
            yield return file;
        }
    }

    /// <inheritdoc/>
    [Obsolete("Use EnumerateExecutableInstancesInDriveAsync instead. (D005)")]
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
    public async Task<FileInfo[]> GetExecutableInstancesInDriveAsync(
        DriveInfo driveInfo,
        string executableName,
        SearchOption directorySearchOption,
        CancellationToken cancellationToken)
        => await EnumerateExecutableInstancesInDriveAsync(
               driveInfo, executableName, directorySearchOption, cancellationToken)
               .ToArrayAsync(cancellationToken: cancellationToken)
               .ConfigureAwait(false);

    /// <inheritdoc/>
    [Obsolete("Use EnumerateExecutableInstancesInDirectoryAsync instead. (D005)")]
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
    public async IAsyncEnumerable<FileInfo> EnumerableExecutableInstancesInDirectoryAsync(
        DirectoryInfo directory,
        string executableName,
        SearchOption directorySearchOption,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (FileInfo file in EnumerateExecutableInstancesInDirectoryAsync(
                           directory, executableName, directorySearchOption, cancellationToken)
                           .ConfigureAwait(false))
        {
            yield return file;
        }
    }

    /// <inheritdoc/>
    [Obsolete("Use EnumerateExecutableInstancesInDirectoryAsync instead. (D005)")]
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
    public async Task<FileInfo[]> GetExecutableInstancesInDirectoryAsync(
        DirectoryInfo directory,
        string executableName,
        SearchOption directorySearchOption,
        CancellationToken cancellationToken)
        => await EnumerateExecutableInstancesInDirectoryAsync(
               directory, executableName, directorySearchOption, cancellationToken)
               .ToArrayAsync(cancellationToken: cancellationToken)
               .ConfigureAwait(false);
}
