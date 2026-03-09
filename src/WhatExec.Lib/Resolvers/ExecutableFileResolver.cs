/*
    WhatExec.Lib
    Copyright (c) 2025-2026 Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Runtime.CompilerServices;
using WhatExec.Lib.Abstractions.Detectors;

namespace WhatExec.Lib;

/// <summary>
/// Represents a class that resolves the location of specified executable files.
/// </summary>
public class ExecutableFileResolver : IExecutableFileResolver
{
    private readonly IExecutableFileDetector _executableFileDetector;
    private readonly IPathEnvironmentVariableResolver _pathEnvironmentVariableResolver;
    
    private readonly StringComparer _stringComparer;
    
    /// <summary>
    /// Represents a class that resolves the location of specified executable files.
    /// </summary>
    /// <param name="executableFileDetector">The executable file detector to use.</param>
    /// <param name="pathEnvironmentVariableResolver">The path environment variable resolver to use.</param>
    public ExecutableFileResolver(IExecutableFileDetector executableFileDetector,
        IPathEnvironmentVariableResolver pathEnvironmentVariableResolver)
    {
        _executableFileDetector = executableFileDetector;
        _pathEnvironmentVariableResolver = pathEnvironmentVariableResolver;
        
        _stringComparer = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
    }

    /// <inheritdoc/>
    public event EventHandler<KeyValuePair<string, FileInfo>>? ExecutableFileLocated;

    /// <summary>
    /// Asynchronously locates the specified executable file within a directory or its subdirectories.
    /// </summary>
    /// <param name="executableFileName">The name of the executable file to locate.</param>
    /// <param name="directorySearchOption">Specifies how directories are searched for the executable file.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that returns a <see cref="FileInfo"/> indicating the location of the executable if found, otherwise null.</returns>
    /// <exception cref="FileNotFoundException">Thrown if the specified executable file could not be found.</exception>
    public async Task<FileInfo> LocateExecutableAsync(string executableFileName, SearchOption directorySearchOption,
        CancellationToken cancellationToken)
    {
        (bool success, FileInfo? file) result = await TryLocateExecutableAsync(executableFileName, directorySearchOption, cancellationToken)
            .ConfigureAwait(false);
        
        if(result is { success: true, file: not null })
            return result.file;
        
        throw new FileNotFoundException(Resources.Exceptions_FileNotFound.Replace("{0}", executableFileName), executableFileName);
    }

    /// <summary>
    /// Attempts to resolve the location of an executable file.
    /// </summary>
    /// <param name="executableFileName">The name of the executable file to locate.</param>
    /// <param name="directorySearchOption">Specifies how directories are searched for the executable file.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that returns a tuple indicating whether the executable was found and its location if successful, otherwise null.</returns>
    public async Task<(bool, FileInfo?)> TryLocateExecutableAsync(string executableFileName,
        SearchOption directorySearchOption,
        CancellationToken cancellationToken)
    {
        (bool foundInPath, KeyValuePair<string, FileInfo>? executable) result = await _pathEnvironmentVariableResolver.
            TryResolveExecutableFilePathAsync(executableFileName, cancellationToken).ConfigureAwait(false);

        if (result.foundInPath && result.executable is not null)
        {
            return (true, result.executable.Value.Value);
        }
        
        StringComparison comparison = OperatingSystem.IsWindows() ?  StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        foreach (DriveInfo drive in DriveInfo.SafelyEnumerateLogicalDrives())
        {
            KeyValuePair<string, FileInfo>[] driveResults = await GetExecutablesInDriveAsync(drive, [executableFileName],
                directorySearchOption, cancellationToken).ConfigureAwait(false);

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
    /// <param name="inputFileNames">The names of the executable files to locate.</param>
    /// <param name="directorySearchOption">Specifies how directories are searched for the executable files.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A dictionary containing the located executable files, where the keys are the original file names and the values are their corresponding <see cref="FileInfo"/> objects.</returns>
    /// <exception cref="FileNotFoundException">Thrown if any of the specified executable files are not found.</exception>
    public async Task<IReadOnlyDictionary<string, FileInfo>> GetExecutableFilesAsync(string[] inputFileNames,
        SearchOption directorySearchOption,
        CancellationToken cancellationToken)
    {
        (bool success, IReadOnlyDictionary<string, FileInfo> executables) result = await TryGetExecutableFilesAsync(inputFileNames,
            directorySearchOption, cancellationToken).ConfigureAwait(false);
        
        if (!result.success && result.executables.Count < inputFileNames.Length)
        {
            string filesNotFound = string.Join(", ", inputFileNames.Except(result.executables.Keys, _stringComparer));
            
            throw new FileNotFoundException(Resources.Exception_FilesNotFound.Replace("{0}", filesNotFound));
        }
        
        return result.executables;
    }

    /// <summary>
    /// Asynchronously enumerates executable files within a directory or its subdirectories.
    /// </summary>
    /// <param name="inputFileNames">The names of the executable files to locate.</param>
    /// <param name="directorySearchOption">Specifies how directories are searched for the executable file.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>An asynchronous sequence of key-value pairs, where each key is a filename and the corresponding value is the location of the executable file if found.</returns>
    public async IAsyncEnumerable<KeyValuePair<string, FileInfo>> EnumerateExecutableFilesAsync(string[] inputFileNames,
        SearchOption directorySearchOption,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        List<string> executablesToLookFor =
#if NET8_0_OR_GREATER
            new(inputFileNames);
#else
            new();
        
        executablesToLookFor.AddRange(inputFileNames);
#endif
        
        IAsyncEnumerable<KeyValuePair<string, FileInfo>> result = _pathEnvironmentVariableResolver.
            EnumerateExecutableFilePathsAsync(inputFileNames, cancellationToken);
        
        await foreach (KeyValuePair<string, FileInfo> kvp in result.ConfigureAwait(false))
        {
            executablesToLookFor.Remove(kvp.Key);
            yield return kvp;
        }

        foreach (DriveInfo drive in DriveInfo.SafelyEnumerateLogicalDrives())
        {
            IAsyncEnumerable<KeyValuePair<string, FileInfo>> driveResults = EnumerateExecutablesInDriveAsync(drive, 
                executablesToLookFor.ToArray(), directorySearchOption, cancellationToken);
            
            await foreach (KeyValuePair<string, FileInfo> driveResult in driveResults.ConfigureAwait(false))
            {
                yield return new KeyValuePair<string, FileInfo>(driveResult.Key, driveResult.Value);
            }
        }
    }

    /// <summary>
    /// Asynchronously attempts to locate specified executable files within a directory or its subdirectories.
    /// </summary>
    /// <param name="inputFileNames">An array of names of the executable files to locate.</param>
    /// <param name="directorySearchOption">Specifies how directories are searched for the executable files.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A tuple containing a boolean indicating whether all executables were found and a dictionary of found executables keyed by their file names.</returns>
    public async Task<(bool, IReadOnlyDictionary<string, FileInfo>)> TryGetExecutableFilesAsync(string[] inputFileNames,
        SearchOption directorySearchOption,
        CancellationToken cancellationToken)
    {
        string[] executablesToLookFor;
        Dictionary<string, FileInfo> output = new(capacity: inputFileNames.Length, _stringComparer);
        
        (bool foundInPath, IReadOnlyDictionary<string, FileInfo> pathExecutables) result = await _pathEnvironmentVariableResolver.
            TryGetExecutableFilePathsAsync(inputFileNames, cancellationToken).ConfigureAwait(false);

        if (result.foundInPath && result.pathExecutables.Count == inputFileNames.Length)
        {
            return (true, result.pathExecutables);
        }
        if(result.pathExecutables.Count > 0)
        {
            foreach (KeyValuePair<string, FileInfo> executable in result.pathExecutables)
            {
                output.Add(executable.Key, executable.Value);
            }
            
            executablesToLookFor = inputFileNames.Where(f => !result.pathExecutables.ContainsKey(f)).ToArray();
        }
        else
        {
            executablesToLookFor = inputFileNames;
        }

        foreach (DriveInfo drive in DriveInfo.SafelyEnumerateLogicalDrives())
        {
            KeyValuePair<string, FileInfo>[] driveResults = await GetExecutablesInDriveAsync(drive, 
                executablesToLookFor, directorySearchOption, cancellationToken).ConfigureAwait(false);
            
            foreach (KeyValuePair<string, FileInfo> driveResult in driveResults)
            {
                bool addSuccess = output.TryAdd(driveResult.Key, driveResult.Value);
            
                if (!addSuccess)
                    output[driveResult.Key] = driveResult.Value;
            }
        }
        
        return (output.Count == inputFileNames.Length, new Dictionary<string, FileInfo>(output, _stringComparer));
    }

    private async Task<KeyValuePair<string, FileInfo>[]> GetExecutablesInDriveAsync(DriveInfo driveInfo,
        string[] inputFileNames, SearchOption directorySearchOption, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(inputFileNames);

        List<KeyValuePair<string, FileInfo>> output = new(capacity: inputFileNames.Length);
        
        StringComparison stringComparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        foreach (string executableFileName in inputFileNames)
        {
            FileInfo? file = driveInfo.RootDirectory.SafelyEnumerateFiles(Path.GetFileName(executableFileName),
                    directorySearchOption)
                .Where(f => executableFileName.Equals(f.Name, stringComparison))
                .FirstOrDefault(f => f.Exists);
            
            if (file is not null)
            {
                bool isExecutable = await _executableFileDetector.IsFileExecutableAsync(file, cancellationToken)
                    .ConfigureAwait(false);

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
        string[] inputFileNames, SearchOption directorySearchOption, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(inputFileNames);
        
        StringComparison stringComparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        foreach (string executableFileName in inputFileNames)
        {
            FileInfo? file = driveInfo.RootDirectory.SafelyEnumerateFiles(Path
                    .GetFileName(executableFileName), directorySearchOption)
                .Where(f => executableFileName.Equals(f.Name, stringComparison))
                .FirstOrDefault(f => f.Exists);
            
            if (file is not null)
            {
                bool isExecutable = await _executableFileDetector.IsFileExecutableAsync(file, cancellationToken)
                    .ConfigureAwait(false);

                if (isExecutable)
                {
                    ExecutableFileLocated?.Invoke(this, new KeyValuePair<string, FileInfo>(executableFileName, file));
                    yield return new KeyValuePair<string, FileInfo>(executableFileName, file);
                }
            }
        }
    }
}