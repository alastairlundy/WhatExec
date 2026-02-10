/*
    WhatExecLib
    Copyright (c) 2025-2026 Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

namespace WhatExecLib;

public class ExecutableFileResolver : IExecutableFileResolver
{
    private readonly IExecutableFileDetector _executableFileDetector;
    private readonly IPathEnvironmentVariableResolver _pathEnvironmentVariableResolver;

    public ExecutableFileResolver(IExecutableFileDetector executableFileDetector, 
        IPathEnvironmentVariableResolver pathEnvironmentVariableResolver)
    {
        _executableFileDetector = executableFileDetector;
        _pathEnvironmentVariableResolver = pathEnvironmentVariableResolver;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="executableFileName"></param>
    /// <param name="directorySearchOption"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<FileInfo> LocateExecutableAsync(string executableFileName, SearchOption directorySearchOption, CancellationToken cancellationToken)
    {
        (bool success, FileInfo? file) result = await TryLocateExecutable(executableFileName, directorySearchOption, cancellationToken);
        
        if(result is { success: true, file: not null })
            return result.file;
        
        throw new FileNotFoundException(Resources.Exceptions_FileNotFound.Replace("{x}", executableFileName));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="executableFileName"></param>
    /// <param name="directorySearchOption"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<(bool, FileInfo?)> TryLocateExecutable(string executableFileName, SearchOption directorySearchOption,
        CancellationToken cancellationToken)
    {
        (bool foundInPath, KeyValuePair<string, FileInfo>? executable) result = await _pathEnvironmentVariableResolver.TryResolveExecutableAsync(executableFileName, cancellationToken);

        if (result.foundInPath && result.executable is not null)
        {
            return (true, result.executable.Value.Value);
        }
        
        StringComparison comparison = OperatingSystem.IsWindows() ?  StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        foreach (DriveInfo drive in DriveInfo.SafelyEnumerateLogicalDrives())
        {
            KeyValuePair<string, FileInfo>[] driveResults = await LocateExecutablesInDriveAsync(drive, [executableFileName], directorySearchOption, cancellationToken);

            foreach (KeyValuePair<string, FileInfo> fileKvp in driveResults)
            {
                if(fileKvp.Value.Name.Equals(executableFileName, comparison))
                    return (true, fileKvp.Value);
            }
        }
        
        return (false, null);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="executableFileNames"></param>
    /// <param name="directorySearchOption"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="FileNotFoundException"></exception>
    public async Task<IReadOnlyDictionary<string, FileInfo>> LocateExecutableFiles(string[] executableFileNames,
        SearchOption directorySearchOption,
        CancellationToken cancellationToken)
    {
        (bool success, IReadOnlyDictionary<string, FileInfo> executables) result = await TryLocateExecutableFilesAsync(executableFileNames,
            directorySearchOption, cancellationToken);
        
        if (!result.success && result.executables.Count < executableFileNames.Length)
        {
            string filesNotFound = string.Join(", ", executableFileNames.Except(result.executables.Keys));
            
            throw new FileNotFoundException(Resources.Exception_FilesNotFound.Replace("{x}", filesNotFound));
        }
        
        return result.executables;
    }

    /// <inheritdoc/>
    public async Task<(bool, IReadOnlyDictionary<string, FileInfo>)> TryLocateExecutableFilesAsync(string[] executableFileNames,
        SearchOption directorySearchOption,
        CancellationToken cancellationToken)
    {
        string[] executablesToLookFor;
        Dictionary<string, FileInfo> output = new(capacity: executableFileNames.Length);
        
        (bool foundInPath, IReadOnlyDictionary<string, FileInfo> pathExecutables) result = await _pathEnvironmentVariableResolver.TryResolveAllExecutableFilePathsAsync(executableFileNames, cancellationToken);

        if (result.foundInPath && result.pathExecutables.Count == executableFileNames.Length)
        {
            return (true, result.pathExecutables);
        }
        if(result.pathExecutables.Count > 0)
        {
            foreach (KeyValuePair<string, FileInfo> executable in result.pathExecutables)
            {
                output.Add(executable.Key, executable.Value);
            }
            
            executablesToLookFor = executableFileNames.Where(f => !result.pathExecutables.ContainsKey(f)).ToArray();
        }
        else
        {
            executablesToLookFor = executableFileNames;
        }

        foreach (DriveInfo drive in DriveInfo.SafelyEnumerateLogicalDrives())
        {
            var driveResults = await LocateExecutablesInDriveAsync(drive, executablesToLookFor, directorySearchOption, cancellationToken);
            
            foreach (KeyValuePair<string, FileInfo> driveResult in driveResults)
            {
                bool addSuccess = output.TryAdd(driveResult.Key, driveResult.Value);
            
                if (!addSuccess)
                    output[driveResult.Key] = driveResult.Value;
            }
        }
        
        return (output.Count == executableFileNames.Length, new Dictionary<string, FileInfo>(output));
    }
    
    private async Task<KeyValuePair<string, FileInfo>[]> LocateExecutablesInDriveAsync(DriveInfo driveInfo,
        string[] executableFileNames, SearchOption directorySearchOption, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(executableFileNames);

        List<KeyValuePair<string, FileInfo>> output = new(capacity: executableFileNames.Length);
        
        StringComparison stringComparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        foreach (string executableFileName in executableFileNames)
        {
            FileInfo? file = driveInfo.RootDirectory.SafelyEnumerateFiles(Path.GetFileName(executableFileName), directorySearchOption)
                .Where(f => executableFileName.Equals(f.Name, stringComparison))
                .FirstOrDefault(f => f.Exists);
            
            if (file is not null)
            {
                bool isExecutable = await _executableFileDetector.IsFileExecutableAsync(file, cancellationToken);
             
                if(isExecutable)
                    output.Add(new KeyValuePair<string, FileInfo>(executableFileName, file));
            }
        }
        
        return output.ToArray();
    }
}