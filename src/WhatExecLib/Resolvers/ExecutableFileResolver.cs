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
    public FileInfo LocateExecutable(string executableFileName, SearchOption directorySearchOption)
    {
        bool success = TryLocateExecutable(executableFileName, directorySearchOption, out FileInfo? fileInfo);
        
        if(success && fileInfo is not null)
            return fileInfo;
        
        throw new FileNotFoundException(Resources.Exceptions_FileNotFound.Replace("{x}", executableFileName));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="executableFileName"></param>
    /// <param name="directorySearchOption"></param>
    /// <param name="resolvedFilePath"></param>
    /// <returns></returns>
    public bool TryLocateExecutable(string executableFileName, SearchOption directorySearchOption, out FileInfo? resolvedFilePath)
    {
        bool foundInPath = _pathEnvironmentVariableResolver.TryResolveExecutable(executableFileName,
            out KeyValuePair<string, FileInfo>? pair);

        if (foundInPath && pair is not null)
        {
            resolvedFilePath = pair.Value.Value;
            return true;
        }
        
        StringComparison comparison = OperatingSystem.IsWindows() ?  StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        FileInfo? output = _storageDriveDetector.EnumerateLogicalDrives()
            .SelectMany(d => LocateExecutablesInDrive(d, [executableFileName], directorySearchOption))
            .Select(f => f.Value)
            .FirstOrDefault(f => f.Name.Equals(executableFileName, comparison));
        
        resolvedFilePath = output;
        
        return output is not null;
    }


    public IReadOnlyDictionary<string, FileInfo> LocateExecutableFiles(SearchOption directorySearchOption, params string[] executableFileNames)
    {
        string[] executablesToLookFor;
        Dictionary<string, FileInfo> output = new(capacity: executableFileNames.Length);

        bool foundInPath =
            _pathEnvironmentVariableResolver.TryResolveAllExecutableFilePaths(executableFileNames, out IReadOnlyDictionary<string, FileInfo> pathExecutables);

        if (foundInPath && pathExecutables.Count == executableFileNames.Length)
        {
            return pathExecutables;
        }
        if (pathExecutables.Count > 0)
        {
            foreach (KeyValuePair<string, FileInfo> pair in pathExecutables)
            {
                output.TryAdd(pair.Key, pair.Value);
            }
            
            executablesToLookFor = executableFileNames.SkipWhile(f => pathExecutables.ContainsKey(f)).ToArray();
        }
        else
        {
            executablesToLookFor = executableFileNames;
        }
        
        IEnumerable<KeyValuePair<string, FileInfo>> results = _storageDriveDetector.EnumerateLogicalDrives()
            .SelectMany(d => LocateExecutablesInDrive(d, executablesToLookFor, directorySearchOption))
            .Where(f => !output.Keys.Contains(f.Key));

        foreach (KeyValuePair<string, FileInfo> result in results)
        {
            output.TryAdd(result.Key, result.Value);
        }
        
        if (output.Count < executableFileNames.Length)
        {
            string filesNotFound = string.Join(", ", executablesToLookFor.Except(executableFileNames));
            
            throw new FileNotFoundException(Resources.Exception_FilesNotFound.Replace("{x}", filesNotFound));
        }
        
        return output;
    }

    /// <inheritdoc/>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("android")]
    public bool TryLocateExecutableFiles(out IReadOnlyDictionary<string, FileInfo> executableFiles, 
        SearchOption directorySearchOption,
        params string[] executableFileNames)
    {
        Dictionary<string, FileInfo> output = new(capacity: executableFileNames.Length);
        
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

        IEnumerable<KeyValuePair<string, FileInfo>> driveResults = _storageDriveDetector.EnumerateLogicalDrives()
            .SelectMany(d => LocateExecutablesInDrive(d, executableFileNames, SearchOption.AllDirectories));

        foreach (KeyValuePair<string, FileInfo> result in driveResults)
        {
            bool addSuccess = output.TryAdd(result.Key, result.Value);
            
            if (!addSuccess)
                output[result.Key] = result.Value;
        }
        
        executableFiles = new Dictionary<string, FileInfo>(output);
        return output.Count == executableFileNames.Length;
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
        
        FileInfo[] files = driveInfo.RootDirectory.SafelyEnumerateFiles("*", directorySearchOption)
            .Where(f => executableFileNames.Any(e => e.Equals(f.Name, stringComparison)))
            .Where(f => f.Exists && _executableFileDetector.IsFileExecutable(f))
            .ToArray();
        
        if(files.Length == 0)
            return [];
        
        return files.Select(f =>
                new KeyValuePair<string, FileInfo>(executableFileNames.First(e => e.Equals(f.Name, stringComparison)),
                    f))
            .ToArray();
    }
}   