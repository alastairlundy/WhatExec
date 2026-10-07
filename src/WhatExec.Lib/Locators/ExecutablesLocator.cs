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
/// One shared traversal core serves all overloads; every file is routed through
/// <see cref="IExecutableFileDetector"/> before yielding.
/// Filesystem access goes through System.IO.Abstractions seam.
/// Fault rules: IgnoreInaccessible (skip per-directory), consistent casing, fixed patterns, no hot tasks.
/// No PATH knowledge; no events on new seam.
/// </summary>
public class ExecutablesLocator : IExecutablesLocator
{
    private readonly IExecutableFileDetector _detector;
    private readonly IFileSystem _fileSystem;

    // Executable-name comparison is OrdinalIgnoreCase by convention (AGENTS.md);
    // never culture-sensitive or platform-dependent.
    private const StringComparison NameComparison = StringComparison.OrdinalIgnoreCase;

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
        [EnumeratorCancellation] CancellationToken ct)
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
        [EnumeratorCancellation] CancellationToken ct)
    {
        await foreach (FileInfo file in EnumerateAcrossDrivesCoreAsync(search, nameFilter: null, ct)
                           .ConfigureAwait(false))
        {
            yield return file;
        }
    }

    // ── Obsolete legacy members ────────────────────────────────────

    /// <inheritdoc/>
    [Obsolete("Use EnumerateExecutablesInDirectoryAsync instead.")]
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
    [Obsolete("Use EnumerateExecutablesInDriveAsync instead.")]
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
    /// The single across-drives fan-out shared by both across-drives entry points.
    /// Enumerates logical drives safely (any enumeration fault yields an empty sequence),
    /// skips drives that are not ready, and traverses each ready root through
    /// <see cref="TraversalCoreAsync"/> - one lazy walk end to end, with no eager
    /// intermediate collections.
    /// </summary>
    /// <param name="search">Top-level only or all subdirectories.</param>
    /// <param name="nameFilter">Optional filename filter (case-insensitive ordinal). Pass null for all-executables.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An async stream of <see cref="FileInfo"/> for matching executable files.</returns>
    internal async IAsyncEnumerable<FileInfo> EnumerateAcrossDrivesCoreAsync(
        SearchOption search,
        string? nameFilter,
        [EnumeratorCancellation] CancellationToken ct)
    {
        DriveInfo[] drives;
        try
        {
            drives = DriveInfo.GetDrives();
        }
        catch (IOException)
        {
            yield break;
        }
        catch (UnauthorizedAccessException)
        {
            yield break;
        }
        catch (System.Security.SecurityException)
        {
            yield break;
        }

        foreach (DriveInfo drive in drives)
        {
            if (!drive.IsReady)
            {
                continue;
            }

            await foreach (FileInfo file in TraversalCoreAsync(drive.RootDirectory.FullName, search, nameFilter, ct)
                               .ConfigureAwait(false))
            {
                yield return file;
            }
        }
    }

    /// <summary>
    /// Shared traversal core serving every location overload - directory, drive, and
    /// across-drives, for both the all-executables and named-instances seams.
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

            FileInfo? file = await TryResolveExecutableAsync(filePath, nameFilter, ct)
                .ConfigureAwait(false);

            if (file is not null)
            {
                yield return file;
            }
        }
    }

    /// <summary>
    /// Maps one traversed path to an executable <see cref="FileInfo"/>, or null when the
    /// entry is skipped: unreadable name, name-filter mismatch, missing seam file, or a
    /// detector fault. Only cancellation propagates.
    /// </summary>
    private async Task<FileInfo?> TryResolveExecutableAsync(
        string filePath,
        string? nameFilter,
        CancellationToken ct)
    {
        string? fileName = GetFileNameSafe(filePath);
        if (fileName is null)
        {
            return null;
        }

        // Name-first filter (5de66e2): cheap name comparison runs before
        // FileInfo construction and the existence probe; never extension-first.
        if (nameFilter is not null &&
            !string.Equals(fileName, nameFilter, NameComparison))
        {
            return null;
        }

        IFileInfo? seamFile = TryGetExistingSeamFile(filePath);
        if (seamFile is null)
        {
            return null;
        }

        FileInfo? file = TryCreateFileInfo(seamFile);
        if (file is null)
        {
            return null;
        }

        return await IsExecutableSafeAsync(file, ct).ConfigureAwait(false) ? file : null;
    }

    /// <summary>
    /// Returns the file name for a path, or null when the name cannot be read or is empty.
    /// </summary>
    private string? GetFileNameSafe(string filePath)
    {
        string fileName;
        try
        {
            fileName = _fileSystem.Path.GetFileName(filePath);
        }
        catch
        {
            return null;
        }

        return string.IsNullOrEmpty(fileName) ? null : fileName;
    }

    /// <summary>
    /// Probes the filesystem seam for an existing file, returning null when the path is
    /// invalid, the probe faults, or the file does not exist.
    /// </summary>
    private IFileInfo? TryGetExistingSeamFile(string filePath)
    {
        IFileInfo seamFile;
        try
        {
            seamFile = _fileSystem.FileInfo.New(filePath);
        }
        catch
        {
            return null;
        }

        try
        {
            return seamFile.Exists ? seamFile : null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Wraps a seam-validated path in a public <see cref="FileInfo"/>, or null when construction faults.
    /// </summary>
    private static FileInfo? TryCreateFileInfo(IFileInfo seamFile)
    {
        try
        {
            return new FileInfo(seamFile.FullName);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Runs the executable detector, returning false (skip the entry) for inaccessible or
    /// vanished files. Cancellation always propagates.
    /// </summary>
    private async Task<bool> IsExecutableSafeAsync(FileInfo file, CancellationToken ct)
    {
        try
        {
            return await _detector.IsFileExecutableAsync(file, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (UnauthorizedAccessException)
        {
            // Skip unauthorized entries.
            return false;
        }
        catch (FileNotFoundException)
        {
            return false;
        }
        catch (DirectoryNotFoundException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }
    }

    /// <summary>
    /// Recursively enumerates file paths from the filesystem seam,
    /// skipping inaccessible directories (IgnoreInaccessible behavior).
    /// Tracks visited directories to avoid symlink cycles.
    /// </summary>
    private IAsyncEnumerable<string> EnumerateFilesRecursiveAsync(
        string directoryPath,
        bool recurse,
        CancellationToken ct)
    {
        StringComparer comparer = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        return EnumerateFilesRecursiveCoreAsync(directoryPath, recurse, ct, new HashSet<string>(comparer));
    }

    private async IAsyncEnumerable<string> EnumerateFilesRecursiveCoreAsync(
        string directoryPath,
        bool recurse,
        [EnumeratorCancellation] CancellationToken ct,
        HashSet<string> visited)
    {
        string normalized;
        try
        {
            normalized = _fileSystem.Path.GetFullPath(directoryPath);
        }
        catch
        {
            yield break;
        }

        if (!visited.Add(normalized))
        {
            yield break;
        }

        // Enumerate files in the current directory, skipping inaccessible entries.
        // Enumeration is lazy, so each MoveNext is guarded: faults surface mid-iteration,
        // not at the call site (IgnoreInaccessible behavior).
        await foreach (string file in DrainWithSkipAsync(
                           () => _fileSystem.Directory.EnumerateFiles(directoryPath, "*"), ct)
                           .ConfigureAwait(false))
        {
            yield return file;
        }

        if (!recurse)
            yield break;

        // Recurse into subdirectories, skipping inaccessible ones.
        await foreach (string subdir in DrainWithSkipAsync(
                           () => _fileSystem.Directory.EnumerateDirectories(directoryPath), ct)
                           .ConfigureAwait(false))
        {
            await foreach (string file in EnumerateFilesRecursiveCoreAsync(subdir, true, ct, visited)
                               .ConfigureAwait(false))
            {
                ct.ThrowIfCancellationRequested();
                yield return file;
            }
        }
    }

    /// <summary>
    /// Drains a lazily-enumerated sequence, ending the sequence (instead of throwing)
    /// when the filesystem reports an inaccessible, missing, or invalid entry mid-iteration.
    /// Cancellation is never swallowed: <see cref="OperationCanceledException"/> propagates.
    /// </summary>
    private static async IAsyncEnumerable<string> DrainWithSkipAsync(
        Func<IEnumerable<string>> enumerate,
        [EnumeratorCancellation] CancellationToken ct)
    {
        IEnumerable<string>? items;
        try
        {
            items = enumerate();
        }
        catch (UnauthorizedAccessException)
        {
            items = null;
        }
        catch (DirectoryNotFoundException)
        {
            items = null;
        }
        catch (IOException)
        {
            items = null;
        }
        catch (System.Security.SecurityException)
        {
            items = null;
        }
        catch (ArgumentException)
        {
            items = null;
        }

        if (items is null)
            yield break;

        using IEnumerator<string> enumerator = items.GetEnumerator();

        while (TryMoveNextWithSkip(enumerator, ct, out string current))
        {
            yield return current;
        }
    }

    /// <summary>
    /// Advances the enumerator, returning false (end of sequence) when the filesystem
    /// reports an inaccessible entry. Only <see cref="OperationCanceledException"/> escapes.
    /// </summary>
    private static bool TryMoveNextWithSkip(
        IEnumerator<string> enumerator,
        CancellationToken ct,
        out string current)
    {
        try
        {
            ct.ThrowIfCancellationRequested();

            if (!enumerator.MoveNext())
            {
                current = string.Empty;
                return false;
            }

            current = enumerator.Current;
            return true;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (UnauthorizedAccessException)
        {
            current = string.Empty;
            return false;
        }
        catch (DirectoryNotFoundException)
        {
            current = string.Empty;
            return false;
        }
        catch (IOException)
        {
            current = string.Empty;
            return false;
        }
        catch (System.Security.SecurityException)
        {
            current = string.Empty;
            return false;
        }
    }
}
