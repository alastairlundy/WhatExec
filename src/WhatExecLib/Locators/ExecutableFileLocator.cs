/*
    WhatExecLib
    Copyright (c) 2025 Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Threading;
using System.Threading.Tasks;
using AlastairLundy.WhatExecLib.Extensions;

namespace AlastairLundy.WhatExecLib.Locators;

public class ExecutableFileLocator : IExecutableFileLocator
{
    private readonly IExecutableFileDetector _executableFileDetector;

    public ExecutableFileLocator(IExecutableFileDetector executableFileDetector)
    {
        _executableFileDetector = executableFileDetector;
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="drive"></param>
    /// <param name="executableFileName"></param>
    /// <param name="directorySearchOption"></param>
    /// <returns></returns>
    public FileInfo? LocateExecutableInDrive(
        DriveInfo drive,
        string executableFileName,
        SearchOption directorySearchOption
    )
    {
        ArgumentException.ThrowIfNullOrEmpty(executableFileName);
        ArgumentNullException.ThrowIfNull(drive);

        if (Path.IsPathRooted(executableFileName))
            return HandleRootedPath(executableFileName);

        FileInfo? result = drive
            .RootDirectory.EnumerateDirectories("*", directorySearchOption)
            .Where(dir => dir.Exists && !dir.IsEmpty)
            .PrioritizeLocations()
            .Select(directory =>
                LocateExecutableInDirectory(directory, executableFileName, directorySearchOption)
            )
            .AsParallel()
            .FirstOrDefault(file => file is not null);

        return result;
    }

    public async Task<FileInfo?> LocateExecutableInDriveAsync(
        DriveInfo drive,
        string executableFileName,
        SearchOption directorySearchOption,
        CancellationToken cancellationToken
    )
    {
        ArgumentException.ThrowIfNullOrEmpty(executableFileName);
        ArgumentNullException.ThrowIfNull(drive);

        return await Task.Run(
            () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                return LocateExecutableInDrive(drive, executableFileName, directorySearchOption);
            },
            cancellationToken
        );
    }

    private FileInfo? HandleRootedPath(string executableFileName)
    {
        try
        {
            if (!File.Exists(executableFileName))
                return null;

            FileInfo file = new FileInfo(executableFileName);

            return _executableFileDetector.IsFileExecutable(file) ? file : null;
        }
        catch (FileNotFoundException)
        {
            return null;
        }
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="directory"></param>
    /// <param name="executableFileName"></param>
    /// <param name="directorySearchOption"></param>
    /// <returns></returns>
    public FileInfo? LocateExecutableInDirectory(
        DirectoryInfo directory,
        string executableFileName,
        SearchOption directorySearchOption
    )
    {
        if (Path.IsPathRooted(executableFileName))
            return HandleRootedPath(executableFileName);

        ArgumentException.ThrowIfNullOrEmpty(executableFileName);
        ArgumentNullException.ThrowIfNull(directory);

        FileInfo? result = directory
            .EnumerateFiles("*", directorySearchOption)
            .Where(file =>
            {
                StringComparison stringComparison = OperatingSystem.IsWindows()
                    ? StringComparison.OrdinalIgnoreCase
                    : StringComparison.Ordinal;

                return file.Exists && file.Name.Equals(executableFileName, stringComparison);
            })
            .FirstOrDefault(file => _executableFileDetector.IsFileExecutable(file));

        return result;
    }

    public async Task<FileInfo?> LocateExecutableInDirectoryAsync(
        DirectoryInfo directory,
        string executableFileName,
        SearchOption directorySearchOption,
        CancellationToken cancellationToken
    )
    {
        ArgumentException.ThrowIfNullOrEmpty(executableFileName);
        ArgumentNullException.ThrowIfNull(directory);

        return await Task.Run(
            () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                return LocateExecutableInDirectory(
                    directory,
                    executableFileName,
                    directorySearchOption
                );
            },
            cancellationToken
        );
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="executableFileName"></param>
    /// <param name="directorySearchOption"></param>
    /// <returns></returns>
    public FileInfo? LocateExecutable(string executableFileName, SearchOption directorySearchOption)
    {
        ArgumentException.ThrowIfNullOrEmpty(executableFileName);

        if (Path.IsPathRooted(executableFileName))
            return HandleRootedPath(executableFileName);

        IEnumerable<DriveInfo> drives = Environment
            .GetLogicalDrives()
            .Select(d => new DriveInfo(d))
            .Where(drive => drive.IsReady);

        FileInfo? result = drives
            .Select(drive =>
                LocateExecutableInDrive(drive, executableFileName, directorySearchOption)
            )
            .AsParallel()
            .FirstOrDefault(file => file is not null);

        return result;
    }

    public async Task<FileInfo?> LocateExecutableAsync(
        string executableFileName,
        SearchOption directorySearchOption,
        CancellationToken cancellationToken
    )
    {
        ArgumentException.ThrowIfNullOrEmpty(executableFileName);

        return await Task.Run(
            () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                return LocateExecutable(executableFileName, directorySearchOption);
            },
            cancellationToken
        );
    }
}
