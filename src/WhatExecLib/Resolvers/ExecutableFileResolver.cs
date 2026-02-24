/*
    WhatExecLib
    Copyright (c) 2025-2026 Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

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
    /// Locates the specified executable file within a directory or its subdirectories.
    /// </summary>
    /// <param name="executableFileName">The name of the executable file to locate.</param>
    /// <param name="directorySearchOption">Specifies how directories are searched for the executable file.</param>
    /// <returns>A <see cref="FileInfo"/> indicating the location of the executable if found, otherwise null.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the specified executable file is not found.</exception>
    public FileInfo LocateExecutable(string executableFileName, SearchOption directorySearchOption)
    {
        (bool success, FileInfo? file) result = TryLocateExecutable(executableFileName, directorySearchOption);

        if(result is { success: true, file: not null })
            return result.file;
        
        throw new FileNotFoundException(Resources.Exceptions_FileNotFound.Replace("{x}", executableFileName));
    }

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
        
        throw new FileNotFoundException(Resources.Exceptions_FileNotFound.Replace("{x}", executableFileName));
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
    /// <param name="executableFileName"></param>
    /// <param name="directorySearchOption"></param>
    /// <returns></returns>
    public (bool, FileInfo?) TryLocateExecutable(string executableFileName, SearchOption directorySearchOption)
    {
        Task<(bool, FileInfo?)> task =
            TryLocateExecutableAsync(executableFileName, directorySearchOption, CancellationToken.None);
        task.Wait();

        return task.Result;
    }

    /// <summary>
    /// Asynchronously locates multiple executable files within a directory or its subdirectories.
    /// </summary>
    /// <param name="executableFileNames">The names of the executable files to locate.</param>
    /// <param name="directorySearchOption">Specifies how directories are searched for the executable files.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A dictionary containing the located executable files, where the keys are the original file names and the values are their corresponding <see cref="FileInfo"/> objects.</returns>
    /// <exception cref="FileNotFoundException">Thrown if any of the specified executable files are not found.</exception>
    public async Task<IReadOnlyDictionary<string, FileInfo>> LocateExecutableFilesAsync(string[] executableFileNames,
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

    /// <summary>
    /// 
    /// </summary>
    /// <param name="executableFileNames"></param>
    /// <param name="directorySearchOption"></param>
    /// <returns></returns>
    /// <exception cref="FileNotFoundException"></exception>
    public IReadOnlyDictionary<string, FileInfo> LocateExecutableFiles(string[] executableFileNames, SearchOption directorySearchOption)
    {
        Task<(bool success, IReadOnlyDictionary<string, FileInfo> executables)> taskResult = TryLocateExecutableFilesAsync(executableFileNames,
            directorySearchOption, CancellationToken.None);

        taskResult.Wait();

        (bool success, IReadOnlyDictionary<string, FileInfo> executables) result = taskResult.Result;
        
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

    /// <summary>
    /// Locates executable files based on specified names and search options.
    /// </summary>
    /// <param name="executableFileNames">The names of the executable files to locate.</param>
    /// <param name="directorySearchOption">Specifies how directories are searched for the executable files.</param>
    /// <returns>A tuple containing a boolean indicating success or failure, and a dictionary mapping executable file names to their corresponding FileInfo objects.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the method fails to locate the specified executable files.</exception>
    public (bool, IReadOnlyDictionary<string, FileInfo>) TryLocateExecutableFiles(string[] executableFileNames,
        SearchOption directorySearchOption)
    {
        Task<(bool, IReadOnlyDictionary<string, FileInfo>)> task = TryLocateExecutableFilesAsync(executableFileNames, directorySearchOption, CancellationToken.None);
        task.Wait();
        
        return task.Result;
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

                if (isExecutable)
                {
                    ExecutableFileLocated?.Invoke(this, new KeyValuePair<string, FileInfo>(executableFileName, file));
                    output.Add(new KeyValuePair<string, FileInfo>(executableFileName, file));
                }
            }
        }
        
        return output.ToArray();
    }
}