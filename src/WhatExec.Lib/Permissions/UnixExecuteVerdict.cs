/*
    WhatExec.Lib
    Copyright (c) 2025-2026 Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.IO.Abstractions;

namespace WhatExec.Lib.Permissions;

/// <summary>
/// Provides the Unix mode-bit execute verdict used as the fallback where no file-format
/// (magic-number) check applies — that is, where there is no PE/MZ, no Mach-O, and no ELF
/// header to consult.
/// </summary>
/// <remarks>
/// The verdict is owner-independent: it asks only whether any of the three execute bits is
/// present in the file's Unix mode. It deliberately differs from the Windows rule, which
/// asks whether the current user holds effective execute access — this check neither reads
/// the file's owner nor consults the caller's identity (no uid/gid, no group membership).
/// </remarks>
internal static class UnixExecuteVerdict
{
    /// <summary>
    /// Determines whether the open stream's file has any Unix execute bit set
    /// (user, group, or other), reading the mode from the open handle rather than by path.
    /// </summary>
    /// <remarks>
    /// This check applies only where no magic-number check applies (no PE/MZ, no Mach-O,
    /// no ELF) — the Unix fallback path. Because the mode is stat'd from the same open
    /// handle, the mode and any header bytes read from that handle describe the same file.
    /// The verdict is owner-independent and therefore deliberately differs from the Windows
    /// current-user rule: the Windows rule asks whether the current user holds effective
    /// execute access, this one asks only whether an execute bit is present — so a file the
    /// calling user cannot execute is still reported when any of the three bits is set.
    /// </remarks>
    /// <param name="stream">The open stream whose file's Unix mode is examined.</param>
    /// <returns>
    /// <see langword="true"/> if any of the user, group, or other execute bits is set;
    /// otherwise <see langword="false"/> (including on Windows, when the stream is not a
    /// real file handle, or when the platform does not support Unix file modes).
    /// </returns>
    internal static bool HasExecutePermission(FileSystemStream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        // Unix file modes are not available on Windows; File.GetUnixFileMode(SafeFileHandle)
        // is [UnsupportedOSPlatform("windows")] and throws there.
        if (OperatingSystem.IsWindows())
            return false;

        try
        {
            // Read the mode from the OPEN HANDLE (never by path) so the mode and the
            // stream's contents describe the same file. The conversion throws
            // InvalidCastException for the mock file system, which carries no Unix mode.
            FileStream handle = (FileStream)stream;

            UnixFileMode mode = File.GetUnixFileMode(handle.SafeFileHandle);

            // Owner-independent: any of the three execute bits suffices.
            return (mode & (UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute)) != UnixFileMode.None;
        }
        catch (InvalidCastException)
        {
            return false;
        }
        catch (PlatformNotSupportedException)
        {
            return false;
        }
    }
}
