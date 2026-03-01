/*
    WhatExecLib
    Copyright (c) 2025-2026 Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Runtime.CompilerServices;
using WhatExec.Lib.Abstractions.Detectors;

namespace WhatExec.Lib;

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

    /// <inheritdoc/>
    public event EventHandler<KeyValuePair<string, FileInfo>>? ExecutableFileLocated;

    /// <summary>
    /// Asynchronously locates the specified executable file within a directory or its subdirectories.
    /// </summary>
    /// <param name="executableFileName">The name of the executable file to locate.</param>
    /// <param name="directorySearchOption">Specifies how directories are searched for the executable file.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task that returns a <see cref="FileInfo"/> indicating the location of the executable if found, otherwise null.</returns>
    public async Task<FileInfo> LocateExecutableAsync(string executableFileName, SearchOption directorySearchOption,
        CancellationToken cancellationToken)
    {
        (bool success, FileInfo? file) result = await TryLocateExecutableAsync(executableFileName, directorySearchOption, cancellationToken);
        
        if(result is { success: true, file: not null })
            return result.file;
        
        throw new FileNotFoundException(Resources.Exceptions_FileNotFound.Replace("{0}", executableFileName));
    }

    /// <summary>
    /// Resolves the location of an executable file.
    /// </summary>
    /// <param name="executableFileName">The name of the executable file to locate.</param>
    /// <param name="directorySearchOption">Specifies how directories are searched for the executable file.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task that returns a tuple indicating whether the executable was found and its location if successful, otherwise null.</returns>
    public async Task<(bool, FileInfo?)> TryLocateExecutableAsync(string executableFileName,
        SearchOption directorySearchOption,
        CancellationToken cancellationToken)
    {
        (bool foundInPath, KeyValuePair<string, FileInfo>? executable) result = await _pathEnvironmentVariableResolver.TryResolveExecutableFilePathAsync(executableFileName, cancellationToken);

        if (result.foundInPath && result.executable is not null)
        {
            return (true, result.executable.Value.Value);
        }
        
        StringComparison comparison = OperatingSystem.IsWindows() ?  StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        foreach (DriveInfo drive in DriveInfo.SafelyEnumerateLogicalDrives())
        {
            KeyValuePair<string, FileInfo>[] driveResults = await GetExecutablesInDriveAsync(drive, [executableFileName], directorySearchOption, cancellationToken);

            foreach (KeyValuePair<string, FileInfo> fileKvp in driveResults)
            {
                if(fileKvp.Value.Name.Equals(executableFileName, comparison))
                    return (true, fileKvp.Value);
            }
        }
        
        return (false, null);
    }

    /// <summary>
    /// Asynchronously locates multiple executable files within a directory or its subdirectories.
    /// </summary>
    /// <param name="executableFileNames">The names of the executable files to locate.</param>
    /// <param name="directorySearchOption">Specifies how directories are searched for the executable files.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A dictionary containing the located executable files, where the keys are the original file names and the values are their corresponding <see cref="FileInfo"/> objects.</returns>
    /// <exception cref="FileNotFoundException">Thrown if any of the specified executable files are not found.</exception>
    public async Task<IReadOnlyDictionary<string, FileInfo>> GetExecutableFilesAsync(string[] executableFileNames,
        SearchOption directorySearchOption,
        CancellationToken cancellationToken)
    {
        (bool success, IReadOnlyDictionary<string, FileInfo> executables) result = await TryGetExecutableFilesAsync(executableFileNames,
            directorySearchOption, cancellationToken);
        
        if (!result.success && result.executables.Count < executableFileNames.Length)
        {
            string filesNotFound = string.Join(", ", executableFileNames.Except(result.executables.Keys));
            
            throw new FileNotFoundException(Resources.Exception_FilesNotFound.Replace("{0}", filesNotFound));
        }
        
        return result.executables;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="executableFileNames"></param>
    /// <param name="directorySearchOption"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async IAsyncEnumerable<KeyValuePair<string, FileInfo>> EnumerateExecutableFilesAsync(string[] executableFileNames, SearchOption directorySearchOption,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        List<string> executablesToLookFor =
#if NET8_0_OR_GREATER
            new(executableFileNames);
#else
            new();
        
        executablesToLookFor.AddRange(executableFileNames);
#endif
        
        IAsyncEnumerable<KeyValuePair<string, FileInfo>> result = _pathEnvironmentVariableResolver.
            EnumerateExecutableFilePathsAsync(executableFileNames, cancellationToken);
        
        await foreach (KeyValuePair<string, FileInfo> kvp in result)
        {
            executablesToLookFor.Remove(kvp.Key);
            yield return kvp;
        }

        foreach (DriveInfo drive in DriveInfo.SafelyEnumerateLogicalDrives())
        {
            IAsyncEnumerable<KeyValuePair<string, FileInfo>> driveResults = EnumerateExecutablesInDriveAsync(drive, 
                executablesToLookFor.ToArray(), directorySearchOption, cancellationToken);
            
            await foreach (KeyValuePair<string, FileInfo> driveResult in driveResults)
            {
                yield return new KeyValuePair<string, FileInfo>(driveResult.Key, driveResult.Value);
            }
        }
    }

    /// <inheritdoc/>
    public async Task<(bool, IReadOnlyDictionary<string, FileInfo>)> TryGetExecutableFilesAsync(string[] executableFileNames,
        SearchOption directorySearchOption,
        CancellationToken cancellationToken)
    {
        string[] executablesToLookFor;
        Dictionary<string, FileInfo> output = new(capacity: executableFileNames.Length);
        
        (bool foundInPath, IReadOnlyDictionary<string, FileInfo> pathExecutables) result = await _pathEnvironmentVariableResolver.
            TryGetExecutableFilePathsAsync(executableFileNames, cancellationToken);

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
            KeyValuePair<string, FileInfo>[] driveResults = await GetExecutablesInDriveAsync(drive, 
                executablesToLookFor, directorySearchOption, cancellationToken);
            
            foreach (KeyValuePair<string, FileInfo> driveResult in driveResults)
            {
                bool addSuccess = output.TryAdd(driveResult.Key, driveResult.Value);
            
                if (!addSuccess)
                    output[driveResult.Key] = driveResult.Value;
            }
        }
        
        return (output.Count == executableFileNames.Length, new Dictionary<string, FileInfo>(output));
    }

    private async Task<KeyValuePair<string, FileInfo>[]> GetExecutablesInDriveAsync(DriveInfo driveInfo,
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

                if (isExecutable)
                {
                    ExecutableFileLocated?.Invoke(this, new KeyValuePair<string, FileInfo>(executableFileName, file));
                    output.Add(new KeyValuePair<string, FileInfo>(executableFileName, file));
                }
            }
        }
        
        return output.ToArray();
    }
    
    
    private async IAsyncEnumerable<KeyValuePair<string, FileInfo>> EnumerateExecutablesInDriveAsync(DriveInfo driveInfo,
        string[] executableFileNames, SearchOption directorySearchOption, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(executableFileNames);
        
        StringComparison stringComparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        foreach (string executableFileName in executableFileNames)
        {
            FileInfo? file = driveInfo.RootDirectory.SafelyEnumerateFiles(Path
                    .GetFileName(executableFileName), directorySearchOption)
                .Where(f => executableFileName.Equals(f.Name, stringComparison))
                .FirstOrDefault(f => f.Exists);
            
            if (file is not null)
            {
                bool isExecutable = await _executableFileDetector.IsFileExecutableAsync(file, cancellationToken);

                if (isExecutable)
                {
                    ExecutableFileLocated?.Invoke(this, new KeyValuePair<string, FileInfo>(executableFileName, file));
                    yield return new KeyValuePair<string, FileInfo>(executableFileName, file);
                }
            }
        }
    }
}