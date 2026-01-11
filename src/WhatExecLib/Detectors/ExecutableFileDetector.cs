/*
    WhatExecLib
    Copyright (c) 2025-2026 Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using DotExtensions.Platforms;

namespace WhatExecLib.Detectors;

/// <summary>
/// Provides functionality to detect whether a file is executable on the current operating system.
/// </summary>
public class ExecutableFileDetector : IExecutableFileDetector
{
    private bool IsUnix { get; init; }

    private bool IsMac { get; init; }

    private bool IsBsdBased { get; init; }

    /// <summary>
    ///
    /// </summary>
    /// <exception cref="PlatformNotSupportedException"></exception>
    public ExecutableFileDetector()
    {
        IsMac = OperatingSystem.IsMacOS() || OperatingSystem.IsMacCatalyst();

        IsUnix = OperatingSystem.IsUnix();

        IsBsdBased = IsMac || OperatingSystem.IsFreeBSD();

        if (OperatingSystem.IsBrowser() || OperatingSystem.IsTvOS() || OperatingSystem.IsIOS())
            throw new PlatformNotSupportedException();
    }

    /// <summary>
    /// Determines whether the specified file can be executed on the current operating system.
    /// </summary>
    /// <param name="file">The file to be checked for executability.</param>
    /// <returns>True if the file is executable, false otherwise.</returns>
    /// <exception cref="FileNotFoundException">Thrown if the specified file does not exist.</exception>
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("android")]
    [UnsupportedOSPlatform("browser")]
    public bool IsFileExecutable(FileInfo file)
    {
        /*
        else if (OperatingSystem.IsLinux())
        {
#pragma warning disable CA1416
            return DoesFileHaveExecutablePermissions(file)
                &&
                //   IsUnixElfFile(fullPath) ||
#pragma warning restore CA1416
        }
        if (IsBsdBased)
        {
#pragma warning disable CA1416
            return DoesFileHaveExecutablePermissions(file)
                ||
                //     IsUnixElfFile(file.FullName) ||
                //    IsMachOFile(file.FullName) ||
#pragma warning restore CA1416
        }*/

        try
        {
            if (!file.Exists)
                throw new FileNotFoundException();

            return file.HasExecutePermission();
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }
}
