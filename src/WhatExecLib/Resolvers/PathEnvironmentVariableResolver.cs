/*
    WhatExecLib
    Copyright (c) 2025-2026 Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using WhatExec.Lib.Abstractions.Detectors;

namespace WhatExec.Lib;

/// <summary>
/// Provides functionality to resolve the path of an executable file based on the system's PATH environment variable.
/// </summary>
public class PathEnvironmentVariableResolver : IPathEnvironmentVariableResolver
{
    private readonly IPathEnvironmentVariableDetector _pathVariableDetector;
    private readonly IExecutableFileDetector _executableFileDetector;

    /// <summary>
    /// Represents a class that resolves file paths based on the system's PATH environment variable.
    /// </summary>
    /// <param name="pathVariableDetector">The path environment variable detector to use.</param>
    /// <param name="executableFileDetector"></param>
    public PathEnvironmentVariableResolver(IPathEnvironmentVariableDetector pathVariableDetector, IExecutableFileDetector executableFileDetector)
    {
        _pathVariableDetector = pathVariableDetector;
        _executableFileDetector = executableFileDetector;
    }

    #region Helper Methods
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
    protected virtual async Task<(bool success, FileInfo? file)> CheckFileExistsAndIsExecutable(
        string filePath,
        CancellationToken cancellationToken)
    {
        if (!Path.IsPathRooted(filePath))
        {
            return (false, null);
        }
            
        if (File.Exists(filePath))
        {
            FileInfo file = new(filePath);

            if (file.Exists && await _executableFileDetector.IsFileExecutableAsync(file, cancellationToken))
            {
                return (true, file);
            }
        }

        return (false, null);
    }

    protected virtual string[] GetPathExtensions()
        => _pathVariableDetector.GetPathFileExtensions();

    protected virtual string[]? GetPathContents()
        => _pathVariableDetector.GetDirectories();
    #endregion

    /// <inheritdoc/>
    public event EventHandler<KeyValuePair<string, FileInfo>>? ExecutableFileLocated;

    /// <summary>
    /// Resolves a file from the system's PATH environment variable using the provided file name.
    /// </summary>
    /// <param name="inputFilePath">The name of the file to resolve, including optional relative or absolute paths.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>A <see cref="FileInfo"/> object representing the resolved file.</returns>
    /// <exception cref="FileNotFoundException">Thrown if the file could not be found.</exception>
    /// <exception cref="PlatformNotSupportedException">Thrown if the current platform is unsupported.</exception>
    /// <exception cref="InvalidOperationException">Thrown if an invalid operation occurs during file resolution, such as PATH not being able to be resolved.</exception>
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
    public async Task<KeyValuePair<string, FileInfo>> ResolveExecutableFilePathAsync(string inputFilePath,
        CancellationToken cancellationToken)
    {
        IAsyncEnumerable<KeyValuePair<string, FileInfo>> result = ResolveAllExecutableFilePathsAsync([inputFilePath], cancellationToken);

        try
        {
            KeyValuePair<string, FileInfo> value =
                await result.FirstAsync(p => p.Key == inputFilePath, cancellationToken);

            return value;
        }
        catch
        {
            throw new FileNotFoundException(Resources.Exceptions_FileNotFound.Replace("{0}", inputFilePath));
        }
    }

    /// <summary>
    /// Resolves a collection of files from the system's PATH environment variable using the provided file name.
    /// </summary>
    /// <param name="inputFilePaths">A collection of file names to resolve, including optional relative or absolute paths.</param>
    /// <param name="cancellationToken">
    /// </param>
    /// <returns>An array of <see cref="FileInfo"/> objects representing the resolved files.</returns>
    /// <exception cref="FileNotFoundException">Thrown if one or more files could not be found in the specified locations.</exception>
    /// <exception cref="PlatformNotSupportedException">Thrown if the current platform is unsupported.</exception>
    /// <exception cref="InvalidOperationException">Thrown if an invalid operation occurs during file resolution, such as PATH not being able to be resolved.</exception>
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
    public IAsyncEnumerable<KeyValuePair<string, FileInfo>> ResolveAllExecutableFilePathsAsync(
        string[] inputFilePaths, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(inputFilePaths);

        string[] pathExtensions = GetPathExtensions();
        string[] pathContents = GetPathContents()
                                ?? throw new InvalidOperationException("PATH Variable could not be found.");

        return InternalResolveFilePaths(inputFilePaths, pathContents, pathExtensions, cancellationToken);
    }

    /// <summary>
    /// Attempts to resolve a file from the system's PATH environment variable using the provided file name.
    /// </summary>
    /// <param name="inputFilePath">The name of the file to resolve, including optional relative or absolute paths.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>True if the file is successfully resolved; otherwise, false.</returns>
    /// <exception cref="PlatformNotSupportedException">Thrown if the current platform is unsupported.</exception>
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
    public async Task<(bool, KeyValuePair<string, FileInfo>?)> TryResolveExecutableAsync(string inputFilePath, CancellationToken cancellationToken)
    {
        (bool success, IReadOnlyDictionary<string, FileInfo> files) result = await TryResolveAllExecutableFilePathsAsync([inputFilePath], cancellationToken);
        
        KeyValuePair<string, FileInfo>? resolvedExecutable = result.files.FirstOrDefault(f => f.Key == inputFilePath);

        return (result.success, resolvedExecutable);
    }

    /// <summary>
    /// Attempts to resolve a set of file paths into executable files based on the system's PATH environment variable.
    /// </summary>
    /// <param name="inputFilePaths">An array of file names or paths to resolve. These can include relative or absolute paths.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>A boolean value indicating whether any of the specified files were successfully resolved.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if resolving the PATH environment variable fails, or an invalid operation occurs during the resolution process.
    /// </exception>
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
    public async Task<(bool, IReadOnlyDictionary<string, FileInfo>)> TryResolveAllExecutableFilePathsAsync(string[] inputFilePaths,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(inputFilePaths);

        string[] pathExtensions = GetPathExtensions();
        string[] pathContents;

        try
        {
            pathContents = GetPathContents()
                           ?? throw new InvalidOperationException("PATH Variable could not be found.");
        }
        catch (InvalidOperationException)
        {
            return (false, new ReadOnlyDictionary<string, FileInfo>(new Dictionary<string, FileInfo>()));
        }

        return await InternalTryResolveFilePathsAsync(inputFilePaths, pathContents, pathExtensions, cancellationToken);
    }

    #region File Resolving Code
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
    protected virtual async IAsyncEnumerable<KeyValuePair<string, FileInfo>> InternalResolveFilePaths(string[] inputFilePaths, string[] pathContents, string[] pathExtensions,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (string inputFilePath in inputFilePaths)
        {
            if (Path.IsPathRooted(inputFilePath)
                || inputFilePath.Contains(Path.DirectorySeparatorChar)
                || inputFilePath.Contains(Path.AltDirectorySeparatorChar))
            {
                (bool success, FileInfo? file) checkResults = await CheckFileExistsAndIsExecutable(inputFilePath, cancellationToken);
                if (checkResults.success && checkResults.file is not null)
                {
                    ExecutableFileLocated?.Invoke(this, new KeyValuePair<string, FileInfo>(inputFilePath, checkResults.file));
                    
                    yield return new KeyValuePair<string, FileInfo>(inputFilePath, checkResults.file);
                    continue;
                }
            }

            bool fileHasExtension = Path.GetExtension(inputFilePath) != string.Empty;

            foreach (string pathEntry in pathContents)
            {
                if (!fileHasExtension && OperatingSystem.IsWindows())
                {
                    foreach (string pathExtension in pathExtensions)
                    {
                        string filePath = Path.Combine(pathEntry,
                            $"{Path.GetFileNameWithoutExtension(inputFilePath)}{pathExtension.ToLower()}");
                        
                        (bool success, FileInfo? file) result = await CheckFileExistsAndIsExecutable(
                            filePath,
                            cancellationToken);

                        if (result.success && result.file is not null)
                        {
                            ExecutableFileLocated?.Invoke(this, new KeyValuePair<string, FileInfo>(inputFilePath, result.file));
                            yield return new KeyValuePair<string, FileInfo>(inputFilePath, result.file);
                        }
                    }
                }
                else
                {
                    string filePath = Path.Combine(pathEntry, Path.GetFileName(inputFilePath));
                    
                    (bool success, FileInfo? file) result = await CheckFileExistsAndIsExecutable(
                        filePath,
                        cancellationToken
                    );

                    if (result.success && result.file is not null)
                    {
                        ExecutableFileLocated?.Invoke(this, new KeyValuePair<string, FileInfo>(inputFilePath, result.file));
                        yield return new KeyValuePair<string, FileInfo>(inputFilePath, result.file);
                    }
                }
            }
        }
    }

    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("tvos")]
    [UnsupportedOSPlatform("browser")]
    protected virtual async Task<(bool, IReadOnlyDictionary<string, FileInfo>)> InternalTryResolveFilePathsAsync(string[] inputFilePaths,
        string[] pathContents, string[] pathExtensions, CancellationToken cancellationToken)
    {
        Dictionary<string, FileInfo> output = new(capacity: inputFilePaths.Length);

        foreach (string inputFilePath in inputFilePaths)
        {
            if (Path.IsPathRooted(inputFilePath)
                || inputFilePath.Contains(Path.DirectorySeparatorChar)
                || inputFilePath.Contains(Path.AltDirectorySeparatorChar))
            {
                (bool success, FileInfo? file) checkResults = await CheckFileExistsAndIsExecutable(inputFilePath, cancellationToken);
                if (checkResults.success && checkResults.file is not null)
                {
                    ExecutableFileLocated?.Invoke(this, new KeyValuePair<string, FileInfo>(inputFilePath, checkResults.file));
                    output.TryAdd(inputFilePath, checkResults.file);
                    continue;
                }
            }

            bool fileHasExtension = Path.GetExtension(inputFilePath) != string.Empty;

            foreach (string pathEntry in pathContents)
            {
                if (!fileHasExtension && OperatingSystem.IsWindows())
                {
                    foreach (string pathExtension in pathExtensions)
                    {
                        string filePath = Path.Combine(pathEntry,
                            $"{Path.GetFileNameWithoutExtension(inputFilePath)}{pathExtension.ToLower()}");
                        
                        (bool success, FileInfo? file) result = await CheckFileExistsAndIsExecutable(filePath, cancellationToken);

                        if (result.success && result.file is not null)
                        {
                            ExecutableFileLocated?.Invoke(this, new KeyValuePair<string, FileInfo>(inputFilePath, result.file));
                            output.TryAdd(inputFilePath, result.file);
                        }
                    }
                }
                else
                {
                    string filePath = Path.Combine(pathEntry, Path.GetFileName(inputFilePath));
                    
                    (bool success, FileInfo? file) result = await CheckFileExistsAndIsExecutable(filePath, cancellationToken);

                    if (result.success && result.file is not null)
                    {
                        ExecutableFileLocated?.Invoke(this, new KeyValuePair<string, FileInfo>(inputFilePath, result.file));
                        output.TryAdd(inputFilePath, result.file);
                    }
                }
            }
        }
        
        return (output.Count != 0, new ReadOnlyDictionary<string, FileInfo>(output));
    }
    #endregion
}