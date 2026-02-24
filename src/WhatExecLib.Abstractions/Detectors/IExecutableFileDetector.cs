/*
    WhatExecLib
    Copyright (c) 2025-2026 Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

namespace WhatExec.Lib.Abstractions.Detectors;

/// <summary>
/// Provides an interface for detecting executable files based on their file type and system permissions.
/// </summary>
public interface IExecutableFileDetector
{
    /// <summary>
    /// Determines whether a given file can be executed.
    /// </summary>
    /// <param name="file">The file to be checked for executability.</param>
    /// <returns>True if the specified file is executable, false otherwise.</returns>
    bool IsFileExecutable(FileInfo file);

    /// <summary>
    /// Determines if a given file is an executable.
    /// </summary>
    /// <param name="file">The file to be checked.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to use to cancel the detection.</param>
    /// <returns>True if the file can be executed, false otherwise.</returns>
    Task<bool> IsFileExecutableAsync(FileInfo file, CancellationToken cancellationToken);
}
