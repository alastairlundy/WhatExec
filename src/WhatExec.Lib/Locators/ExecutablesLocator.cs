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
/// Locates all executable files within directories, drives, or across drives.
/// One shared traversal core serves all overloads (D016); every file is routed through
/// <see cref="IExecutableFileDetector"/> before yielding (D003, D014).
/// Filesystem access goes through System.IO.Abstractions seam (D009).
/// Fault rules: IgnoreInaccessible (skip per-directory), consistent casing, fixed patterns, no hot tasks (D010).
/// No PATH knowledge (D002, D011); no events on new seam (D008).
/// </summary>
public class ExecutablesLocator : IExecutablesLocator
{
    private readonly IExecutableFileDetector _detector;
    private readonly IFileSystem _fileSystem;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExecutablesLocator"/> class.
    /// </summary>
    /// <param name="detector">The executable file detector used to verify executability per file.</param>
    /// <param name="fileSystem">The filesystem abstraction for all file and directory access.</param>
    public ExecutablesLocator(IExecutableFileDetector detector, IFileSystem fileSystem)
    {
        _detector = detector;
        _fileSystem = fileSystem;
    }

    // ── IExecutablesLocator new seam ──────────────────────────────────────

    /// <inheritdoc/>
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
    public async IAsyncEnumerable<FileInfo> EnumerateExecutablesInDirectoryAsync(
        DirectoryInfo directory,
        SearchOption search,
        [EnumeratorCancellation] CancellationToken ct)
    {
        await foreach (FileInfo file in TraversalCoreAsync(directory.FullName, search, nameFilter: null, ct)
                           .ConfigureAwait(false))
        {
            yield return file;
        }
    }

    /// <inheritdoc/>
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
    public async IAsyncEnumerable<FileInfo> EnumerateExecutablesInDriveAsync(
        DriveInfo drive,
        SearchOption search,
        CancellationToken ct)
    {
        if (!drive.IsReady)
            throw new ArgumentException(
                string.Format(Resources.Exceptions_Drives_DriveNotReady, drive.Name),
                nameof(drive));

        await foreach (FileInfo file in TraversalCoreAsync(drive.RootDirectory.FullName, search, nameFilter: null, ct)
                           .ConfigureAwait(false))
        {
            yield return file;
        }
    }

    /// <inheritdoc/>
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
    public async IAsyncEnumerable<FileInfo> EnumerateExecutablesAcrossDrivesAsync(
        SearchOption search,
        CancellationToken ct)
    {
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

            await foreach (FileInfo file in TraversalCoreAsync(drive.RootDirectory.FullName, search, nameFilter: null, ct)
                               .ConfigureAwait(false))
            {
                yield return file;
            }
        }
    }

    // ── Obsolete legacy members (D005) ────────────────────────────────────

    /// <inheritdoc/>
    [Obsolete("Use EnumerateExecutablesInDirectoryAsync instead. (D005)")]
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
    public async Task<FileInfo[]> GetExecutablesInDirectoryAsync(
        DirectoryInfo directory,
        SearchOption directorySearchOption,
        CancellationToken cancellationToken)
        => await EnumerateExecutablesInDirectoryAsync(directory, directorySearchOption, cancellationToken)
            .ToArrayAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc/>
    [Obsolete("Use EnumerateExecutablesInDriveAsync instead. (D005)")]
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
    public async Task<FileInfo[]> GetExecutablesInDriveAsync(
        DriveInfo driveInfo,
        SearchOption directorySearchOption,
        CancellationToken cancellationToken)
        => await EnumerateExecutablesInDriveAsync(driveInfo, directorySearchOption, cancellationToken)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);

    // ── Shared traversal core ─────────────────────────────────────────────

    /// <summary>
    /// Shared traversal core serving all six overloads (D016).
    /// Enumerates files from the given root path using the <see cref="IFileSystem"/> seam,
    /// routes each through <see cref="IExecutableFileDetector.IsFileExecutableAsync"/>,
    /// and yields matches.
    /// </summary>
    /// <param name="rootPath">The starting directory path for traversal.</param>
    /// <param name="search">Top-level only or all subdirectories.</param>
    /// <param name="nameFilter">Optional filename filter (case-insensitive ordinal). Pass null for all-executables.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An async stream of <see cref="FileInfo"/> for matching executable files.</returns>
    internal async IAsyncEnumerable<FileInfo> TraversalCoreAsync(
        string rootPath,
        SearchOption search,
        string? nameFilter,
        [EnumeratorCancellation] CancellationToken ct)
    {
        bool recurse = search == SearchOption.AllDirectories;

        await foreach (string filePath in EnumerateFilesRecursiveAsync(rootPath, recurse, ct)
                           .ConfigureAwait(false))
        {
            ct.ThrowIfCancellationRequested();

            FileInfo file = new FileInfo(filePath);

            // Fixed name filter — no extension-first dead pattern (D010).
            if (nameFilter is not null &&
                !string.Equals(file.Name, nameFilter, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            bool isExecutable;
            try
            {
                isExecutable = await _detector.IsFileExecutableAsync(file, ct).ConfigureAwait(false);
            }
            catch (UnauthorizedAccessException)
            {
                // Skip unauthorized entries (D010).
                continue;
            }

            if (isExecutable)
            {
                yield return file;
            }
        }
    }

    /// <summary>
    /// Recursively enumerates file paths from the filesystem seam,
    /// skipping inaccessible directories (IgnoreInaccessible behavior, D010).
    /// </summary>
    private async IAsyncEnumerable<string> EnumerateFilesRecursiveAsync(
        string directoryPath,
        bool recurse,
        [EnumeratorCancellation] CancellationToken ct)
    {
        // Enumerate files in the current directory, skipping inaccessible entries.
        IEnumerable<string> files;
        try
        {
            files = _fileSystem.Directory.EnumerateFiles(directoryPath, "*");
        }
        catch (UnauthorizedAccessException)
        {
            yield break;
        }
        catch (IOException)
        {
            yield break;
        }

        foreach (string file in files)
        {
            ct.ThrowIfCancellationRequested();
            yield return file;
        }

        if (!recurse)
            yield break;

        // Recurse into subdirectories, skipping inaccessible ones.
        IEnumerable<string> subdirectories;
        try
        {
            subdirectories = _fileSystem.Directory.EnumerateDirectories(directoryPath);
        }
        catch (UnauthorizedAccessException)
        {
            yield break;
        }
        catch (IOException)
        {
            yield break;
        }

        foreach (string subdir in subdirectories)
        {
            await foreach (string file in EnumerateFilesRecursiveAsync(subdir, true, ct)
                               .ConfigureAwait(false))
            {
                ct.ThrowIfCancellationRequested();
                yield return file;
            }
        }
    }
}
