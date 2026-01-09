/*
    WhatExecLib
    Copyright (c) 2025 Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using DotPrimitives.IO.Paths;

// ReSharper disable ConvertClosureToMethodGroup

namespace WhatExecLib;

/// <summary>
/// Provides functionality to resolve the path of an executable file based on the system's PATH environment variable.
/// </summary>
public class PathExecutableResolver : IPathExecutableResolver
{
    protected bool IsUnix { get; }

    /// <summary>
    ///
    /// </summary>
    public PathExecutableResolver()
    {
        IsUnix =
            OperatingSystem.IsLinux()
            || OperatingSystem.IsMacOS()
            || OperatingSystem.IsMacCatalyst()
            || OperatingSystem.IsFreeBSD()
            || OperatingSystem.IsAndroid();
    }

    #region Helper Methods
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("android")]
    protected bool ExecutableFileIsValid(string filePath, out FileInfo? fileInfo)
    {
        try
        {
            string fullFilePath = Path.GetFullPath(filePath);

            FileInfo file = new FileInfo(fullFilePath);

            if (IsUnix)
            {
                if (file.HasExecutePermission())
                {
                    fileInfo = file;
                    return true;
                }
            }
            else
            {
                if (File.Exists(fullFilePath))
                {
                    fileInfo = file;
                    return true;
                }
            }

            fileInfo = null;
            return false;
        }
        catch (ArgumentException)
        {
            fileInfo = null;
            return false;
        }
    }

    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("android")]
    protected bool CheckFileExists(
        string inputFilePath,
        out FileInfo? fileInfo,
        string pathEntry,
        string pathExtension
    )
    {
        string filePath;
        
        if (Path.HasExtension(inputFilePath))
        {
            filePath = Path.Combine(pathEntry, 
                $"{Path.GetFileNameWithoutExtension(inputFilePath)}{pathExtension}");
        }
        else
        {
            filePath = Path.Combine(pathEntry, $"{inputFilePath}{pathExtension}");
        }

        if (File.Exists(filePath))
        {
            if (ExecutableFileIsValid(filePath, out FileInfo? info) && info is not null)
            {
                fileInfo = info;
                return File.Exists(Path.GetFullPath(filePath));
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
    public FileInfo GetResolvedExecutable(string inputFilePath) =>
        EnumerateResolvedExecutables([inputFilePath]).First();

    /// <summary>
    ///
    /// </summary>
    /// <param name="inputFilePaths"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="FileNotFoundException"></exception>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("android")]
    public IEnumerable<FileInfo> EnumerateResolvedExecutables(IEnumerable<string> inputFilePaths)
    {
        ArgumentNullException.ThrowIfNull(inputFilePaths);

        string[] pathExtensions = PathEnvironmentVariable.GetPathFileExtensions();
        string[] pathContents =
            PathEnvironmentVariable.GetDirectories()
            ?? throw new InvalidOperationException("PATH Variable could not be found.");

        bool foundAny = false;

        foreach (string inputFilePath in inputFilePaths)
        {
            if (
                Path.IsPathRooted(inputFilePath)
                || inputFilePath.Contains(Path.DirectorySeparatorChar)
                || inputFilePath.Contains(Path.AltDirectorySeparatorChar)
            )
            {
                if (File.Exists(inputFilePath))
                {
                    if (
                        ExecutableFileIsValid(inputFilePath, out FileInfo? info) && info is not null
                    )
                    {
                        foundAny = true;
                        yield return info;
                    }
                }
            }

            bool fileHasExtension = Path.GetExtension(inputFilePath) != string.Empty;

            foreach (string pathEntry in pathContents)
            {
                if (!fileHasExtension)
                {
                    pathExtensions = [""];
                }

                foreach (string pathExtension in pathExtensions)
                {
                    bool result = CheckFileExists(
                        inputFilePath,
                        out FileInfo? fileInfo,
                        pathEntry,
                        pathExtension
                    );

                    if (result && fileInfo is not null)
                    {
                        foundAny = true;
                        yield return fileInfo;
                    }
                }
            }
        }

        if (!foundAny)
            throw new FileNotFoundException($"Could not find file(s) in {nameof(inputFilePaths)}");
    }

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
    public FileInfo[] GetResolvedExecutables(params string[] inputFilePaths) =>
        EnumerateResolvedExecutables(inputFilePaths).ToArray();

    /// <summary>
    /// Attempts to resolve a file from the system's PATH environment variable using the provided file name.
    /// </summary>
    /// <param name="inputFilePath">The name of the file to resolve, including optional relative or absolute paths.</param>
    /// <param name="fileInfo">When this method returns, contains the resolved <see cref="FileInfo"/>
    /// object if the resolution is successful; otherwise, null.</param>
    /// <returns>True if the file is successfully resolved; otherwise, false.</returns>
    /// <exception cref="PlatformNotSupportedException">Thrown if the current platform is unsupported.</exception>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("android")]
    public bool TryResolveExecutable(string inputFilePath, out FileInfo? fileInfo)
    {
        bool success = TryResolveExecutables([inputFilePath], out FileInfo[]? fileInfos);

        fileInfo = fileInfos?.FirstOrDefault() ?? null;

        return success;
    }

    /// <summary>
    /// Attempts to resolve a set of file paths into executable files based on the system's PATH environment variable.
    /// </summary>
    /// <param name="inputFilePaths">An array of file names or paths to resolve. These can include relative or absolute paths.</param>
    /// <param name="fileInfos">
    /// When the method completes, contains an array of <see cref="FileInfo"/> objects representing the resolved files,
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
    public bool TryResolveExecutables(string[] inputFilePaths, out FileInfo[]? fileInfos)
    {
        ArgumentNullException.ThrowIfNull(inputFilePaths);

        string[] pathExtensions = PathEnvironmentVariable.GetPathFileExtensions();
        string[] pathContents;

        try
        {
            pathContents =
                PathEnvironmentVariable.GetDirectories()
                ?? throw new InvalidOperationException("PATH Variable could not be found.");
        }
        catch (InvalidOperationException)
        {
            fileInfos = null;
            return false;
        }

        bool foundAny = false;
        List<FileInfo> output = new();

        foreach (string inputFilePath in inputFilePaths)
        {
            if (Path.IsPathRooted(inputFilePath)
                || inputFilePath.Contains(Path.DirectorySeparatorChar)
                || inputFilePath.Contains(Path.AltDirectorySeparatorChar))
            {
                if (File.Exists(inputFilePath))
                {
                    if (
                        ExecutableFileIsValid(inputFilePath, out FileInfo? info) && info is not null
                    )
                    {
                        output.Add(info);
                        foundAny = true;
                    }
                }
            }

            bool fileHasExtension = Path.GetExtension(inputFilePath) != string.Empty;

            foreach (string pathEntry in pathContents)
            {
                if (!fileHasExtension)
                {
                    pathExtensions = [""];
                }

                foreach (string pathExtension in pathExtensions)
                {
                    bool result = CheckFileExists(
                        inputFilePath,
                        out FileInfo? fileInfo,
                        pathEntry,
                        pathExtension
                    );

                    if (result && fileInfo is not null)
                    {
                        foundAny = true;
                        output.Add(fileInfo);
                    }
                }
            }
        }

        if (foundAny)
        {
            fileInfos = output.ToArray();
            return true;
        }

        fileInfos = null;
        return false;
    }
}
