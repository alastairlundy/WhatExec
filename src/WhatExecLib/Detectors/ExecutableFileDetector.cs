/*
    WhatExecLib
    Copyright (c) 2025 Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.IO;
using System.Runtime.Versioning;
using AlastairLundy.DotExtensions.IO.Permissions;
using AlastairLundy.WhatExecLib.Abstractions.Detectors;

namespace AlastairLundy.WhatExecLib.Detectors;

/// <summary>
/// Provides functionality to detect whether a file is executable on the current operating system.
/// </summary>
public class ExecutableFileDetector : IExecutableFileDetector
{
    private bool IsUnix { get; set; }

    private bool IsBsdBased { get; set; }

    /// <summary>
    ///
    /// </summary>
    /// <exception cref="PlatformNotSupportedException"></exception>
    public ExecutableFileDetector()
    {
        IsUnix =
            OperatingSystem.IsLinux()
            || OperatingSystem.IsMacOS()
            || OperatingSystem.IsMacCatalyst()
            || OperatingSystem.IsFreeBSD()
            || OperatingSystem.IsAndroid();

        IsBsdBased =
            OperatingSystem.IsMacOS()
            || OperatingSystem.IsMacCatalyst()
            || OperatingSystem.IsFreeBSD();

        if (OperatingSystem.IsBrowser() || OperatingSystem.IsTvOS() || OperatingSystem.IsIOS())
            throw new PlatformNotSupportedException();
    }

    /// <summary>
    /// Determines whether the specified file can be executed on the current operating system.
    /// </summary>
    /// <param name="file">The file to be checked for executability.</param>
    /// <returns>True if the file is executable, false otherwise.</returns>
    /// <exception cref="FileNotFoundException">Thrown if the specified file does not exist.</exception>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("android")]
    [UnsupportedOSPlatform("browser")]
    public bool IsFileExecutable(FileInfo file)
    {
        if (!file.Exists)
            throw new FileNotFoundException();

        /*
        if (OperatingSystem.IsWindows())
        {
            return DoesFileHaveExecutablePermissions(file);
        }
        else if (OperatingSystem.IsLinux())
        {
#pragma warning disable CA1416
            return DoesFileHaveExecutablePermissions(file)
                &&
                //   IsUnixElfFile(fullPath) ||
                DoesFileHaveExecutableExtension(file);
#pragma warning restore CA1416
        }
        if (IsBsdBased)
        {
#pragma warning disable CA1416
            return DoesFileHaveExecutablePermissions(file)
                ||
                //     IsUnixElfFile(file.FullName) ||
                //    IsMachOFile(file.FullName) ||
                DoesFileHaveExecutableExtension(file);
#pragma warning restore CA1416
        }*/

        return file.HasExecutePermission();
    }
}
