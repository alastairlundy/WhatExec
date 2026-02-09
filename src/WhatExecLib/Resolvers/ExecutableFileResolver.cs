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
    /// <returns></returns>
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

        FileInfo? output = DriveInfo.SafelyEnumerateLogicalDrives()
            .SelectMany(d => LocateExecutablesInDrive(d, [executableFileName], directorySearchOption))
            .Select(f => f.Value)
            .FirstOrDefault(f => f.Name.Equals(executableFileName, comparison));
        
        resolvedFilePath = output;
        
        return output is not null;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="directorySearchOption"></param>
    /// <param name="executableFileNames"></param>
    /// <returns></returns>
    /// <exception cref="FileNotFoundException"></exception>
    public IReadOnlyDictionary<string, FileInfo> LocateExecutableFiles(SearchOption directorySearchOption, params string[] executableFileNames)
    {
        bool success = TryLocateExecutableFiles(out IReadOnlyDictionary<string, FileInfo> output, directorySearchOption,
            executableFileNames);
        
        if (!success && output.Count < executableFileNames.Length)
        {
            string filesNotFound = string.Join(", ", executableFileNames.Except(output.Keys));
            
            throw new FileNotFoundException(Resources.Exception_FilesNotFound.Replace("{x}", filesNotFound));
        }
        
        return output;
    }

    /// <inheritdoc/>
    public bool TryLocateExecutableFiles(out IReadOnlyDictionary<string, FileInfo> executableFiles, 
        SearchOption directorySearchOption,
        params string[] executableFileNames)
    {
        string[] executablesToLookFor;
        Dictionary<string, FileInfo> output = new(capacity: executableFileNames.Length);
        
        bool foundInPath = _pathEnvironmentVariableResolver.TryResolveAllExecutableFilePaths(executableFileNames, 
            out IReadOnlyDictionary<string, FileInfo> pathExecutables);

        if (foundInPath && pathExecutables.Count == executableFileNames.Length)
        {
            executableFiles = pathExecutables;
            return true;
        }
        if(pathExecutables.Count > 0)
        {
            foreach (KeyValuePair<string, FileInfo> result in pathExecutables)
            {
                output.Add(result.Key, result.Value);
            }
            
            executablesToLookFor = executableFileNames.Where(f => !pathExecutables.ContainsKey(f)).ToArray();
        }
        else
        {
            executablesToLookFor = executableFileNames;
        }
        
        IEnumerable<KeyValuePair<string, FileInfo>> driveResults = DriveInfo.SafelyEnumerateLogicalDrives()
            .SelectMany(d => LocateExecutablesInDrive(d, executablesToLookFor, directorySearchOption));

        foreach (KeyValuePair<string, FileInfo> result in driveResults)
        {
            bool addSuccess = output.TryAdd(result.Key, result.Value);
            
            if (!addSuccess)
                output[result.Key] = result.Value;
        }
        
        executableFiles = new Dictionary<string, FileInfo>(output);
        return output.Count == executableFileNames.Length;
    }
    
    private KeyValuePair<string, FileInfo>[] LocateExecutablesInDrive(DriveInfo driveInfo,
        string[] executableFileNames, SearchOption directorySearchOption)
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
                .FirstOrDefault(f => f.Exists && _executableFileDetector.IsFileExecutableAsync(f, CancellationToken.None).Result);
           
            if(file is not null)
                output.Add(new KeyValuePair<string, FileInfo>(executableFileName, file));
        }
        
        return output.ToArray();
    }
}