/*
    XPlatWhereLib
    Copyright (c) 2025 Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.IO;
using System.Runtime.Versioning;

using AlastairLundy.DotPrimitives.IO.Permissions;
using AlastairLundy.DotPrimitives.IO.Permissions.Windows;

using WhatExecLib.Abstractions;

namespace WhatExecLib.Detectors;

/// <summary>
/// Provides functionality to detect whether a file is executable on the current operating system.
/// </summary>
public class ExecutableFileDetector : IExecutableFileDetector
{
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
        if (File.Exists(file.FullName) == false)
            throw new FileNotFoundException();
       
        if (OperatingSystem.IsWindows())
        {
            return DoesFileHaveExecutablePermissions(file) &&
                   DoesFileHaveExecutableExtension(file);
        }
        else if (OperatingSystem.IsLinux())
        {
#pragma warning disable CA1416
            return DoesFileHaveExecutablePermissions(file) ||
                   //   IsUnixElfFile(fullPath) || 
                   DoesFileHaveExecutableExtension(file);
#pragma warning restore CA1416
        }

        if (OperatingSystem.IsMacOS() || OperatingSystem.IsMacCatalyst() ||  OperatingSystem.IsFreeBSD())
        {
#pragma warning disable CA1416
            return DoesFileHaveExecutablePermissions(file) ||
                   //     IsUnixElfFile(file.FullName) || 
                   //    IsMachOFile(file.FullName) ||
                   DoesFileHaveExecutableExtension(file);
#pragma warning restore CA1416
        }
        
        return DoesFileHaveExecutablePermissions(file) ||
               DoesFileHaveExecutableExtension(file);
    }

    /// <summary>
    /// Determines if a given file has executable permissions on the current operating system.
    /// </summary>
    /// <param name="file">The file for which to check executable permissions.</param>
    /// <returns>True if the file has executable permissions; otherwise, false.</returns>
    /// <exception cref="FileNotFoundException">Thrown if the file specified does not exist.</exception>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("android")]
    [UnsupportedOSPlatform("browser")]
    public bool DoesFileHaveExecutablePermissions(FileInfo file)
    {
        if (File.Exists(file.FullName) == false)
            throw new FileNotFoundException();
        
        if (OperatingSystem.IsWindows())
        {
            WindowsFilePermission filePermission = WindowsFilePermissionManager.GetFilePermission(file.FullName);
            
           return filePermission.HasExecutePermission();
        }
        else if (OperatingSystem.IsLinux() ||
                 OperatingSystem.IsMacOS() || OperatingSystem.IsMacCatalyst()
                 || OperatingSystem.IsFreeBSD()
                 || OperatingSystem.IsAndroid())
        {
#pragma warning disable CA1416
            UnixFileMode fileMode = File.GetUnixFileMode(file.FullName);
#pragma warning restore CA1416

            return fileMode.HasFlag(UnixFileMode.OtherExecute) ||
                   fileMode.HasFlag(UnixFileMode.GroupExecute) ||
                   fileMode.HasFlag(UnixFileMode.UserExecute);
        }

        return false;
    }

    /// <summary>
    /// Determines whether a specified file has a valid executable file extension.
    /// </summary>
    /// <param name="file">The file to be checked.</param>
    /// <returns>True if the file extension is valid for an executable, false otherwise.</returns>
    /// <exception cref="FileNotFoundException">Thrown if the file specified does not exist.</exception>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("ios")]
    [SupportedOSPlatform("android")]
    [UnsupportedOSPlatform("browser")]
    public bool DoesFileHaveExecutableExtension(FileInfo file)
    {
        if (File.Exists(file.FullName) == false)
            throw new FileNotFoundException();
        
        bool output = false;
        
        if (OperatingSystem.IsWindows())
        {
            output = file.Extension switch
            {
                ".exe" => true,
                ".msi" => true,
                ".appx" => true,
                ".com" => true,
                ".bat" => true,
                ".cmd" => true,
                ".jar" => true,
                _ => false
            };
        }
        else if (OperatingSystem.IsLinux())
        {
            output = file.Extension switch
            {
                ".appimage" => true,
                ".deb" => true,
                ".rpm" => true,
                ".so" => true,
                ".o" => true,
                ".out" => true,
                ".bin" => true,
                ".elf" => true,
                ".mod" => true,
                ".axf" => true,
                ".ko" => true,
                ".prx" => true,
                ".puff" => true,
                ".jar" => true,
                ".sh" => true,
                _ => false
            };
        }
        else if (OperatingSystem.IsMacOS() || OperatingSystem.IsMacCatalyst())
        {
            output = file.Extension switch
            {
                ".kext" => true,
                ".pkg" => true,
                ".app" => true,
                ".so" => true,
                ".o" => true,
                ".out" => true,
                ".bin" => true,
                ".elf" => true,
                ".mod" => true,
                ".axf" => true,
                ".ko" => true,
                ".prx" => true,
                ".puff" => true,
                ".jar" => true,
                ".sh" => true,
                _ => false
            };
        }
        else if(OperatingSystem.IsFreeBSD())
        {
            output = file.Extension switch
            {
                ".appimage" => true,
                ".so" => true,
                ".o" => true,
                ".out" => true,
                ".bin" => true,
                ".elf" => true,
                ".mod" => true,
                ".axf" => true,
                ".ko" => true,
                ".prx" => true,
                ".puff" => true,
                ".jar" => true,
                ".sh" => true,
                _ => false
            };
        }
        else if(OperatingSystem.IsAndroid())
        {
            output = file.Extension switch
            {
                ".apk" => true,
                ".aab" => false,
                ".so" => true,
                ".o" => true,
                ".out" => true,
                ".bin" => true,
                ".elf" => true,
                ".mod" => true,
                ".axf" => true,
                ".ko" => true,
                ".prx" => true,
                ".puff" => true,
                ".jar" => true,
                ".sh" => true,
                _ => false
            };
        }
        else if (OperatingSystem.IsIOS())
        {
            output = file.Extension switch
            {
                ".ipa" => true,
                _ => false
            };
        }
        
        return output;
    }
}