/*
    WhatExecLib
    Copyright (c) 2025-2026 Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using WhatExecLib.Localizations;

namespace WhatExecLib;

public class ExecutableFileResolver : IExecutableFileResolver
{
    private readonly IExecutableFileDetector _executableFileDetector;
    private readonly IPathEnvironmentVariableResolver _pathEnvironmentVariableResolver;
    private readonly IStorageDriveDetector _storageDriveDetector;

    public ExecutableFileResolver(IExecutableFileDetector executableFileDetector, 
        IPathEnvironmentVariableResolver pathEnvironmentVariableResolver)
    {
        _executableFileDetector = executableFileDetector;
        _pathEnvironmentVariableResolver = pathEnvironmentVariableResolver;
        _storageDriveDetector = StorageDrives.Shared;
    }

    public ExecutableFileResolver(IExecutableFileDetector executableFileDetector, 
        IPathEnvironmentVariableResolver pathEnvironmentVariableResolver,
        IStorageDriveDetector storageDriveDetector)
    {
        _executableFileDetector = executableFileDetector;
        _pathEnvironmentVariableResolver = pathEnvironmentVariableResolver;
        _storageDriveDetector = storageDriveDetector;
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
        bool results = TryLocateExecutableFiles(out IReadOnlyDictionary<string, FileInfo> files, executableFileName);

        return results ? files.First().Value : null;
    }
    
    /// <inheritdoc/> 
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
            throw new FileNotFoundException(Resources.Exception_FilesNotFound.Replace("{x}", $": {string.Join(",", executableFileNames)}"));

        if (!executableFiles.Keys.Equals(executableFileNames))
            throw new FileNotFoundException(Resources.Exception_FilesNotFound.Replace("{x}", $": {string.Join(",", executableFileNames)}"));

        return executableFiles;
    }
    
    /// <inheritdoc/>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("android")]
    public bool TryLocateExecutableFiles(out IReadOnlyDictionary<string, FileInfo> executableFiles, 
        params string[] executableFileNames)
    {
        Dictionary<string, FileInfo> output = new();
        
        bool foundInPath = _pathEnvironmentVariableResolver.TryResolveAllExecutableFilePaths(executableFileNames, 
            out IReadOnlyDictionary<string, FileInfo> resolvedExecutables);

        if (foundInPath && resolvedExecutables.Count == executableFileNames.Length)
        {
            executableFiles = resolvedExecutables;
            return true;
        }
        else if(resolvedExecutables.Count > 0)
        {
            foreach (KeyValuePair<string, FileInfo> result in resolvedExecutables)
            {
                output.Add(result.Key, result.Value);
            }
        }

        foreach (DriveInfo drive in _storageDriveDetector.EnumerateLogicalDrives())
        {
            KeyValuePair<string, FileInfo>[] driveResults = LocateExecutablesInDrive(drive, 
                executableFileNames, SearchOption.AllDirectories);

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