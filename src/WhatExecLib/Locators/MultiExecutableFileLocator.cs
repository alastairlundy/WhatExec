/*
    WhatExecLib
    Copyright (c) 2025-2026 Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

namespace WhatExecLib.Locators;

/// <summary>
/// 
/// </summary>
public class MultiExecutableFileLocator : IMultiExecutableFileLocator
{
    private readonly IExecutableFileDetector _executableFileDetector;
    private readonly IStorageDriveDetector _storageDriveDetector;

    public MultiExecutableFileLocator(IExecutableFileDetector executableFileDetector)
    {
        _executableFileDetector = executableFileDetector;
        _storageDriveDetector = StorageDrives.Shared;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="executableFileDetector"></param>
    /// <param name="storageDriveDetector"></param>
    public MultiExecutableFileLocator(IExecutableFileDetector executableFileDetector,
        IStorageDriveDetector storageDriveDetector)
    {
        _executableFileDetector = executableFileDetector;
        _storageDriveDetector = storageDriveDetector;
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="executableFileNames"></param>
    /// <returns></returns>
    /// <exception cref="FileNotFoundException"></exception>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("android")]
    public IReadOnlyDictionary<string, FileInfo> LocateExecutableFiles(params string[] executableFileNames)
    {
        ArgumentNullException.ThrowIfNull(executableFileNames);

        bool success = TryLocateExecutableFiles(out IReadOnlyDictionary<string, FileInfo> executableFiles, executableFileNames);
        
        if(!success)
            throw new FileNotFoundException("Could not locate the specified executable files.");

        if (!executableFiles.Keys.Equals(executableFileNames))
            throw new FileNotFoundException("Could not locate all specified executable files.");

        return executableFiles;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="executableFiles"></param>
    /// <param name="executableFileNames"></param>
    /// <returns></returns>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("android")]
    public bool TryLocateExecutableFiles(out IReadOnlyDictionary<string, FileInfo> executableFiles, 
        params string[] executableFileNames)
    {
        Dictionary<string, FileInfo> output = new();

        foreach (DriveInfo drive in _storageDriveDetector.EnumeratePhysicalDrives())
        {
            KeyValuePair<string, FileInfo>[] driveResults = LocateExecutablesInDrive(drive, 
                executableFileNames, SearchOption.TopDirectoryOnly);

            foreach (KeyValuePair<string, FileInfo> result in driveResults)
            {
                bool added = output.TryAdd(result.Key, result.Value);

                if (!added)
                {
                    output[result.Key] = result.Value;
                }
            }
        }

        executableFiles = new Dictionary<string, FileInfo>(output);
        return output.Count != 0;
    }

    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("android")]
    private KeyValuePair<string, FileInfo>[] LocateExecutablesInDrive(DriveInfo driveInfo,
        string[] executableFileNames, SearchOption directorySearchOption)
    {
        ArgumentNullException.ThrowIfNull(executableFileNames);
        
        StringComparison stringComparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        return executableFileNames.Select(executable =>
            {
                FileInfo? result = executable
                    .GetSearchPatterns()
                    .SelectMany(sp => driveInfo.RootDirectory.SafelyEnumerateFiles(sp, directorySearchOption))
                    .PrioritizeLocations()
                    .FirstOrDefault(f =>
                    {
                        try
                        {
                            return f.Exists && f.Name.Equals(executable, stringComparison)
                                            && _executableFileDetector.IsFileExecutable(f);
                        }
                        catch
                        {
                            // Ignore per-file errors and continue scanning
                            return false;
                        }
                    });

                return new KeyValuePair<string, FileInfo?>(executable, result);
            })
            .Where(f => f.Value is not null)
            // ReSharper disable once NullableWarningSuppressionIsUsed
            .Select(f => new KeyValuePair<string,FileInfo>(f.Key, f.Value!))
            .ToArray();
    }
}