/*
    WhatExecLib
    Copyright (c) 2025 Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using WhatExecLib.Helpers;

namespace WhatExecLib.Locators;

public class ExecutableFileLocator : IExecutableFileLocator
{
    private readonly IExecutableFileDetector _executableFileDetector;
    private readonly IStorageDriveDetector _storageDriveDetector;

    public ExecutableFileLocator(IExecutableFileDetector executableFileDetector)
    {
        _executableFileDetector = executableFileDetector;
        _storageDriveDetector = StorageDrives.Shared;
    }

    public ExecutableFileLocator(IExecutableFileDetector executableFileDetector, 
        IStorageDriveDetector storageDriveDetector)
    {
        _executableFileDetector = executableFileDetector;
        _storageDriveDetector = storageDriveDetector;
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="drive"></param>
    /// <param name="executableFileName"></param>
    /// <param name="directorySearchOption"></param>
    /// <returns></returns>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("android")]
    public FileInfo? LocateExecutableInDrive(
        DriveInfo drive,
        string executableFileName,
        SearchOption directorySearchOption)
    {
        ArgumentException.ThrowIfNullOrEmpty(executableFileName);
        ArgumentNullException.ThrowIfNull(drive);

        if (Path.IsPathRooted(executableFileName))
            return FileLocatorHelper.HandleRootedPath(_executableFileDetector, executableFileName);

        StringComparison stringComparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        IEnumerable<string> searchPatterns = executableFileName.GetSearchPatterns();

        FileInfo? result = searchPatterns
            .SelectMany(sp =>
                drive.RootDirectory.SafelyEnumerateFiles(sp, SearchOption.AllDirectories)
            )
            .PrioritizeLocations()
            .FirstOrDefault(f =>
            {
                try
                {
                    return f.Exists
                           && f.Name.Equals(executableFileName, stringComparison)
                           && _executableFileDetector.IsFileExecutable(f);
                }
                catch
                {
                    // Ignore per-file errors and continue scanning
                    return false;
                }
            });

        return result;
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="directory"></param>
    /// <param name="executableFileName"></param>
    /// <param name="directorySearchOption"></param>
    /// <returns></returns>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("android")]
    public FileInfo? LocateExecutableInDirectory(
        DirectoryInfo directory,
        string executableFileName,
        SearchOption directorySearchOption
    )
    {
        if (Path.IsPathRooted(executableFileName))
            return FileLocatorHelper.HandleRootedPath(_executableFileDetector, executableFileName);

        ArgumentException.ThrowIfNullOrEmpty(executableFileName);
        ArgumentNullException.ThrowIfNull(directory);

        IEnumerable<string> searchPatterns = executableFileName.GetSearchPatterns();

        StringComparison stringComparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        FileInfo? result = searchPatterns
            .SelectMany(sp => directory.SafelyEnumerateFiles(sp, SearchOption.AllDirectories))
            .PrioritizeLocations()
            .Where(f => f.Exists)
            .FirstOrDefault(file =>
                file.Exists
                && file.Name.Equals(executableFileName, stringComparison)
                && _executableFileDetector.IsFileExecutable(file)
            );

        return result;
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="executableFileName"></param>
    /// <param name="directorySearchOption"></param>
    /// <returns></returns>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("android")]
    public FileInfo? LocateExecutable(string executableFileName, SearchOption directorySearchOption)
    {
        ArgumentException.ThrowIfNullOrEmpty(executableFileName);

        if (Path.IsPathRooted(executableFileName))
            return FileLocatorHelper.HandleRootedPath(_executableFileDetector, executableFileName);
        
        IEnumerable<DriveInfo> drives = _storageDriveDetector.EnumeratePhysicalDrives();

        return drives
            .Select(d => LocateExecutableInDrive(d, executableFileName, directorySearchOption))
            .FirstOrDefault(x => x is not null);
    }
}
