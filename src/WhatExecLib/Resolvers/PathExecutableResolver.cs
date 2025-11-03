/*
    WhatExecLib
    Copyright (c) 2025 Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.IO;
using System.Linq;
using AlastairLundy.WhatExecLib.Abstractions;
using AlastairLundy.WhatExecLib.Abstractions.Detectors;
#if NETSTANDARD2_0
using OperatingSystem = Polyfills.OperatingSystemPolyfill;
#endif

// ReSharper disable ConvertClosureToMethodGroup

namespace AlastairLundy.WhatExecLib;

/// <summary>
/// Provides functionality to resolve the path of an executable file based on the system's PATH environment variable.
/// </summary>
public class PathExecutableResolver : IPathExecutableResolver
{
    private readonly IExecutableFileDetector _executableFileDetector;
    private bool IsUnix { get; }
    private bool IsWindows { get; }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="executableFileDetector"></param>
    public PathExecutableResolver(IExecutableFileDetector executableFileDetector)
    {
        _executableFileDetector = executableFileDetector;
        IsUnix = OperatingSystem.IsLinux() || OperatingSystem.IsMacOS() || OperatingSystem.IsMacCatalyst() ||
                 OperatingSystem.IsFreeBSD() || OperatingSystem.IsAndroid();
        IsWindows = OperatingSystem.IsWindows();
    }

    private string[] GetPathExtensions()
    {
        string[]? pathExtensions;

        if (IsUnix)
        {
            pathExtensions = [".sh", ""];
        }
        else if (IsWindows)
        {
            char pathSeparator = ';';
            pathExtensions = Environment.GetEnvironmentVariable("PATHEXT")
                                 ?.Split(pathSeparator, StringSplitOptions.RemoveEmptyEntries)
                                 .Select(x =>
                                 {
                                     x = x.Trim();
                                     x = x.Trim('"');
                                     if (x.StartsWith('.') == false)
                                        x = x.Insert(0, ".");
                                     
                                     return x;
                                 }).ToArray() ?? 
                             [".COM", ".EXE", ".BAT", ".CMD"];
        }
        else
        {
            throw new PlatformNotSupportedException();
        }

        return pathExtensions
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private string[] GetPathContents()
    {
        string? pathContents;
        char pathSeparator;

        if (IsUnix)
        {
            pathContents = Environment.GetEnvironmentVariable("PATH");
            pathSeparator = ':';
        }
        else if (IsWindows)
        {
            pathContents = Environment.GetEnvironmentVariable("PATH");
            pathSeparator = ';';
        }
        else
        {
            throw new PlatformNotSupportedException();
        }

        if (pathContents is null)
            throw new InvalidOperationException("PATH Variable could not be found.");
        
        return pathContents.Split(pathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p.Trim())
            .Select(p => Environment.ExpandEnvironmentVariables(p))
            .Select(x =>
            {
                x = x.Trim('"');
                const string homeToken = "$HOME";
                string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

                int homeTokenIndex = x.IndexOf(homeToken, StringComparison.CurrentCultureIgnoreCase);
                
                if (x.StartsWith('~'))
                {
                    x = $"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}{x.Substring(1)}";
                }
                if (homeTokenIndex != -1)
                {
                    return $"{x.Substring(0, homeTokenIndex)}{userProfile}{x.Substring(homeTokenIndex + homeToken.Length)}";
                }

                x = x.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                
                return x;
            })
            .ToArray();
    }
    
    protected bool ExecutableFileIsValid(string filePath, out FileInfo? fileInfo)
    {
        try
        {
            string fullFilePath = Path.GetFullPath(filePath);

            FileInfo file = new FileInfo(fullFilePath);

            if (IsUnix)
            {
                if (_executableFileDetector.DoesFileHaveExecutablePermissions(file))
                {
                    fileInfo = file;
                    return true;
                }

                fileInfo = null;
                return false;
            }
            // ReSharper disable once RedundantIfElseBlock
            else
            {
                if (File.Exists(fullFilePath))
                {
                    fileInfo = file;
                    return true;
                }

                fileInfo = null;
                return false;
            }
        }
        catch (ArgumentException)
        {
            fileInfo = null;
            return false;
        }
    }

    /// <summary>
    /// Resolves a file from the system's PATH environment variable using the provided file name.
    /// </summary>
    /// <param name="inputFilePath">The name of the file to resolve, including optional relative or absolute paths.</param>
    /// <returns>A <see cref="FileInfo"/> object representing the resolved file.</returns>
    /// <exception cref="FileNotFoundException">Thrown if the file could not be found in the specified locations.</exception>
    /// <exception cref="PlatformNotSupportedException">Thrown if the current platform is unsupported.</exception>
    /// <exception cref="InvalidOperationException">Thrown if an invalid operation occurs during file resolution, such as PATH not being able to be resolved.</exception>
    public FileInfo ResolvePathEnvironmentFile(string inputFilePath)
    {
        bool result = TryResolvePathEnvironmentFile(inputFilePath, out FileInfo? fileInfo);
        
        if(result == false || fileInfo is null)
            throw new FileNotFoundException($"Could not find file: {inputFilePath}");
        
        if(File.Exists(fileInfo.FullName) == false)
            throw new FileNotFoundException($"Could not find file: {inputFilePath}");

        return fileInfo;
    }

    /// <summary>
    /// Attempts to resolve a file from the system's PATH environment variable using the provided file name.
    /// </summary>
    /// <param name="inputFilePath">The name of the file to resolve, including optional relative or absolute paths.</param>
    /// <param name="fileInfo">When this method returns, contains the resolved <see cref="FileInfo"/> object if the resolution is successful; otherwise, null.</param>
    /// <returns>True if the file is successfully resolved; otherwise, false.</returns>
    /// <exception cref="PlatformNotSupportedException">Thrown if the current platform is unsupported.</exception>
    public bool TryResolvePathEnvironmentFile(string inputFilePath, out FileInfo? fileInfo)
    {
#if NET8_0_OR_GREATER
        ArgumentException.ThrowIfNullOrEmpty(inputFilePath, nameof(inputFilePath));
#endif

        if (Path.IsPathRooted(inputFilePath) || inputFilePath.Contains(Path.DirectorySeparatorChar) ||
            inputFilePath.Contains(Path.AltDirectorySeparatorChar))
        {
            if (File.Exists(inputFilePath))
            {
                if (ExecutableFileIsValid(inputFilePath, out FileInfo? info) && info is not null)
                {
                    fileInfo = info;
                    return true;
                }
            }

            fileInfo = null;
            return false;
        }
        
        bool fileHasExtension = Path.GetExtension(inputFilePath) != string.Empty;

        string[] pathExtensions = GetPathExtensions();
        string[] pathContents;

        try
        {
            pathContents = GetPathContents();
        }
        catch (InvalidOperationException)
        {
            fileInfo = null;
            return false;
        }
        
        foreach (string pathEntry in pathContents)
        {
            if (fileHasExtension == false)
            {
                foreach (string pathExtension in pathExtensions)
                {
                    string filePath = Path.Combine(pathEntry, $"{inputFilePath}{pathExtension}");
                    
                    if (File.Exists(filePath))
                    {
                        if (ExecutableFileIsValid(filePath, out FileInfo? info)  && info is not null)
                        {
                            fileInfo = info;
                            return File.Exists(Path.GetFullPath(filePath));
                        }
                    }
                }
            }
            else
            {
                string filePath = Path.Combine(pathEntry, inputFilePath);

                if (File.Exists(filePath))
                {
                    if (ExecutableFileIsValid(filePath, out FileInfo? info)  && info is not null)
                    {
                        fileInfo = info;
                        return File.Exists(Path.GetFullPath(filePath));
                    }
                }
            }
        }
        
        fileInfo = null;
        return false;
    }
}