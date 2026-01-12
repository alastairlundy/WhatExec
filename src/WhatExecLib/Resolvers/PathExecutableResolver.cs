/*
    WhatExecLib
    Copyright (c) 2025 Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Collections.ObjectModel;
using DotPrimitives.IO.Paths;

// ReSharper disable ConvertClosureToMethodGroup

namespace WhatExecLib;

/// <summary>
/// Provides functionality to resolve the path of an executable file based on the system's PATH environment variable.
/// </summary>
public class PathExecutableResolver : IPathExecutableResolver
{
    /// <summary>
    ///
    /// </summary>
    public PathExecutableResolver()
    {
    }

    #region Helper Methods
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("android")]
    protected bool CheckFileExistsAndIsExecutable(
        string filePath,
        out FileInfo? fileInfo)
    {
        string fullFilePath = Path.GetFullPath(filePath);

        bool fullPathRequired = !File.Exists(filePath) && File.Exists(fullFilePath);
        
        if (File.Exists(fullFilePath) || File.Exists(filePath))
        {
            string newFilePath = fullPathRequired ? fullFilePath : filePath;
            FileInfo file = new(newFilePath);

            if (file.Exists && file.HasExecutePermission())
            {
                fileInfo = file;
                return true;
            }
        }

        fileInfo = null;
        return false;
    }
    #endregion

    /// <summary>
    /// Resolves a file from the system's PATH environment variable using the provided file name.
    /// </summary>
    /// <param name="inputFilePath">The name of the file to resolve, including optional relative or absolute paths.</param>
    /// <returns>A <see cref="FileInfo"/> object representing the resolved file.</returns>
    /// <exception cref="FileNotFoundException">Thrown if the file could not be found.</exception>
    /// <exception cref="PlatformNotSupportedException">Thrown if the current platform is unsupported.</exception>
    /// <exception cref="InvalidOperationException">Thrown if an invalid operation occurs during file resolution, such as PATH not being able to be resolved.</exception>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("android")]
    public KeyValuePair<string, FileInfo> ResolveExecutableFilePath(string inputFilePath) =>
        ResolveAllExecutableFilePaths([inputFilePath]).First(p => p.Key == inputFilePath);

    /// <summary>
    /// Resolves a collection of files from the system's PATH environment variable using the provided file name.
    /// </summary>
    /// <param name="inputFilePaths">A collection of file names to resolve, including optional relative or absolute paths.</param>
    /// <returns>An array of <see cref="FileInfo"/> objects representing the resolved files.</returns>
    /// <exception cref="FileNotFoundException">Thrown if one or more files could not be found in the specified locations.</exception>
    /// <exception cref="PlatformNotSupportedException">Thrown if the current platform is unsupported.</exception>
    /// <exception cref="InvalidOperationException">Thrown if an invalid operation occurs during file resolution, such as PATH not being able to be resolved.</exception>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("android")]
    public IReadOnlyDictionary<string, FileInfo> ResolveAllExecutableFilePaths(params string[] inputFilePaths)
    {
        ArgumentNullException.ThrowIfNull(inputFilePaths);

        string[] pathExtensions = PathEnvironmentVariable.GetPathFileExtensions();
        string[] pathContents = PathEnvironmentVariable.GetDirectories()
                                ?? throw new InvalidOperationException("PATH Variable could not be found.");

        Dictionary<string, FileInfo> output = new(capacity: inputFilePaths.Length);

        foreach (string inputFilePath in inputFilePaths)
        {
            if (
                Path.IsPathRooted(inputFilePath)
                || inputFilePath.Contains(Path.DirectorySeparatorChar)
                || inputFilePath.Contains(Path.AltDirectorySeparatorChar))
            {
                if (CheckFileExistsAndIsExecutable(inputFilePath, out FileInfo? fileInfo) && fileInfo is not null)
                {
                    output.Add(inputFilePath, fileInfo);
                }
            }

            bool fileHasExtension = Path.GetExtension(inputFilePath) != string.Empty;

            foreach (string pathEntry in pathContents)
            {
                if (fileHasExtension)
                {
                    foreach (string pathExtension in pathExtensions)
                    {
                        string filePath = Path.Combine(pathEntry, Path.HasExtension(inputFilePath) ? 
                            $"{Path.GetFileNameWithoutExtension(inputFilePath)}{pathExtension}" : $"{inputFilePath}{pathExtension}");

                    
                        bool result = CheckFileExistsAndIsExecutable(
                            filePath,
                            out FileInfo? fileInfo
                        );

                        if (result && fileInfo is not null)
                        {
                            output.Add(filePath, fileInfo);
                        }
                    }
                }
                else
                {
                    string filePath = Path.Combine(pathEntry, Path.HasExtension(inputFilePath) ? 
                        $"{Path.GetFileNameWithoutExtension(inputFilePath)}" : $"{inputFilePath}");

                    
                    bool result = CheckFileExistsAndIsExecutable(
                        filePath,
                        out FileInfo? fileInfo
                    );

                    if (result && fileInfo is not null)
                    {
                        output.Add(filePath, fileInfo);
                    }
                }
            }
            
            bool found = output.ContainsKey(inputFilePath);
            
            if(!found)
                throw new FileNotFoundException($"Could not resolve file path of {inputFilePath}.");
        }
        
        return new ReadOnlyDictionary<string, FileInfo>(output);
    }

    /// <summary>
    /// Attempts to resolve a file from the system's PATH environment variable using the provided file name.
    /// </summary>
    /// <param name="inputFilePath">The name of the file to resolve, including optional relative or absolute paths.</param>
    /// <param name="resolvedExecutable">When this method returns, contains the resolved <see cref="FileInfo"/>
    /// object if the resolution is successful; otherwise, null.</param>
    /// <returns>True if the file is successfully resolved; otherwise, false.</returns>
    /// <exception cref="PlatformNotSupportedException">Thrown if the current platform is unsupported.</exception>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("android")]
    public bool TryResolveExecutable(string inputFilePath, out KeyValuePair<string, FileInfo>? resolvedExecutable)
    {
        bool success = TryResolveAllExecutableFilePaths([inputFilePath], out IReadOnlyDictionary<string, FileInfo> fileInfos);

        resolvedExecutable = fileInfos.FirstOrDefault(f => f.Key == inputFilePath);
       
        return success;
    }

    /// <summary>
    /// Attempts to resolve a set of file paths into executable files based on the system's PATH environment variable.
    /// </summary>
    /// <param name="inputFilePaths">An array of file names or paths to resolve. These can include relative or absolute paths.</param>
    /// <param name="resolvedExecutables">When the method completes, contains an array of <see cref="FileInfo"/> objects representing the resolved files,
    /// if any files are successfully resolved. Null if no files are resolved.
    /// </param>
    /// <returns>A boolean value indicating whether any of the specified files were successfully resolved.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if resolving the PATH environment variable fails, or an invalid operation occurs during the resolution process.
    /// </exception>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("android")]
    public bool TryResolveAllExecutableFilePaths(string[] inputFilePaths, out IReadOnlyDictionary<string, FileInfo> resolvedExecutables)
    {
        ArgumentNullException.ThrowIfNull(inputFilePaths);

        string[] pathExtensions = PathEnvironmentVariable.GetPathFileExtensions();
        string[] pathContents;

        try
        {
            pathContents = PathEnvironmentVariable.GetDirectories()
                           ?? throw new InvalidOperationException("PATH Variable could not be found.");
        }
        catch (InvalidOperationException)
        {
            resolvedExecutables = new ReadOnlyDictionary<string, FileInfo>(new Dictionary<string, FileInfo>());
            return false;
        }

        Dictionary<string, FileInfo> output = new(capacity: inputFilePaths.Length);

        foreach (string inputFilePath in inputFilePaths)
        {
            if (Path.IsPathRooted(inputFilePath)
                || inputFilePath.Contains(Path.DirectorySeparatorChar)
                || inputFilePath.Contains(Path.AltDirectorySeparatorChar))
            {
                if (CheckFileExistsAndIsExecutable(inputFilePath, out FileInfo? fileInfo) && fileInfo is not null)
                {
                    output.Add(inputFilePath, fileInfo);
                }
            }

            bool fileHasExtension = Path.GetExtension(inputFilePath) != string.Empty;

            foreach (string pathEntry in pathContents)
            {
                if (fileHasExtension)
                {
                    foreach (string pathExtension in pathExtensions)
                    {
                        string filePath = Path.Combine(pathEntry, Path.HasExtension(inputFilePath) ? 
                            $"{Path.GetFileNameWithoutExtension(inputFilePath)}{pathExtension}" : $"{inputFilePath}{pathExtension}");
                        
                        bool result = CheckFileExistsAndIsExecutable(filePath, out FileInfo? fileInfo);

                        if (result && fileInfo is not null)
                        {
                            output.Add(filePath, fileInfo);
                        }
                    }
                }
                else
                {
                    string filePath = Path.Combine(pathEntry, Path.HasExtension(inputFilePath) ? 
                        $"{Path.GetFileNameWithoutExtension(inputFilePath)}" : $"{inputFilePath}");
                    
                    bool result = CheckFileExistsAndIsExecutable(filePath, out FileInfo? fileInfo);

                    if (result && fileInfo is not null)
                    {
                        output.Add(filePath, fileInfo);
                    }
                }
            }
        }
        
        resolvedExecutables = new ReadOnlyDictionary<string, FileInfo>(output);
        return output.Count != 0;
    }
}
