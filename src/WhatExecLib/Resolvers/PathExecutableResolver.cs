/*
    WhatExecLib
    Copyright (c) 2025 Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using AlastairLundy.DotExtensions.IO.Permissions;
using AlastairLundy.DotPrimitives.IO.Paths;

// ReSharper disable ConvertClosureToMethodGroup

namespace AlastairLundy.WhatExecLib;

/// <summary>
/// Provides functionality to resolve the path of an executable file based on the system's PATH environment variable.
/// </summary>
public class PathExecutableResolver : IPathExecutableResolver
{
    private readonly IExecutableFileDetector _executableFileDetector;
    protected bool IsUnix { get; }
    protected bool IsWindows { get; }

    /// <summary>
    ///
    /// </summary>
    /// <param name="executableFileDetector"></param>
    public PathExecutableResolver(IExecutableFileDetector executableFileDetector)
    {
        _executableFileDetector = executableFileDetector;
        IsUnix =
            OperatingSystem.IsLinux()
            || OperatingSystem.IsMacOS()
            || OperatingSystem.IsMacCatalyst()
            || OperatingSystem.IsFreeBSD()
            || OperatingSystem.IsAndroid();
        IsWindows = OperatingSystem.IsWindows();
    }

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
            // ReSharper disable once RedundantIfElseBlock
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

    /// <summary>
    /// Resolves a file from the system's PATH environment variable using the provided file name.
    /// </summary>
    /// <param name="inputFilePath">The name of the file to resolve, including optional relative or absolute paths.</param>
    /// <returns>A <see cref="FileInfo"/> object representing the resolved file.</returns>
    /// <exception cref="FileNotFoundException">Thrown if the file could not be found in the specified locations.</exception>
    /// <exception cref="PlatformNotSupportedException">Thrown if the current platform is unsupported.</exception>
    /// <exception cref="InvalidOperationException">Thrown if an invalid operation occurs during file resolution, such as PATH not being able to be resolved.</exception>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("android")]
    public FileInfo ResolvePathEnvironmentExecutableFile(string inputFilePath)
    {
        bool result = TryResolvePathEnvironmentExecutableFile(
            inputFilePath,
            out FileInfo? fileInfo
        );

        bool fileDoesntExist = !File.Exists(fileInfo?.FullName);

        if (!result || fileInfo is null || fileDoesntExist)
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
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("android")]
    public bool TryResolvePathEnvironmentExecutableFile(
        string inputFilePath,
        out FileInfo? fileInfo
    )
    {
#if NET8_0_OR_GREATER
        ArgumentException.ThrowIfNullOrEmpty(inputFilePath, nameof(inputFilePath));
#endif

        if (
            Path.IsPathRooted(inputFilePath)
            || inputFilePath.Contains(Path.DirectorySeparatorChar)
            || inputFilePath.Contains(Path.AltDirectorySeparatorChar)
        )
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
            fileInfo = null;
            return false;
        }

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
                    out fileInfo,
                    pathEntry,
                    pathExtension
                );

                if (result)
                    return result;
            }
        }

        fileInfo = null;
        return false;
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
        string filePath = Path.Combine(pathEntry, $"{inputFilePath}{pathExtension}");

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
}
