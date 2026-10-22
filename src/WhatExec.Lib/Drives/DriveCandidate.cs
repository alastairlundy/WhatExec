/*
    WhatExec.Lib
    Copyright (c) 2025-2026 Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

namespace WhatExec.Lib.Drives;

/// <summary>
/// Provides WhatExec's own rule for deciding whether a logical drive is a Drive candidate
/// (a logical drive that WhatExec has admitted for traversal).
/// </summary>
/// <remarks>
/// Admission inspects the drive's root path string only: no size query, existence probe,
/// or root open happens at admission, so a disconnected network mapping and an empty
/// removable volume are excluded without a blocking stat. The readiness read is
/// point-in-time, so a drive that turns unready after admission yields no results rather
/// than an error, because the traversal fault policy absorbs the failure.
/// </remarks>
internal static class DriveCandidate
{
    /// <summary>
    /// Determines whether the specified drive is a Drive candidate (a logical drive that
    /// WhatExec has admitted for traversal).
    /// </summary>
    /// <remarks>
    /// Admission inspects <see cref="DriveInfo.RootDirectory"/>/<see cref="System.IO.Path"/>
    /// path text only: no size query, existence probe, or root open happens at admission.
    /// The readiness read is point-in-time, so a drive that turns unready after admission
    /// yields no results rather than an error, because the traversal fault policy absorbs
    /// the failure.
    /// </remarks>
    /// <param name="drive">The drive to evaluate.</param>
    /// <returns>
    /// <see langword="true"/> if the drive is ready and its root path is non-empty and
    /// rooted; otherwise <see langword="false"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="drive"/> is <see langword="null"/>.</exception>
    internal static bool IsCandidate(DriveInfo drive)
    {
        ArgumentNullException.ThrowIfNull(drive);

        if (!drive.IsReady)
            return false;

        string rootPath;
        try
        {
            // The drive can turn unready between the readiness read and this root read.
            rootPath = drive.RootDirectory.FullName;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }

        return rootPath.Length > 0 && Path.IsPathRooted(rootPath);
    }
}
