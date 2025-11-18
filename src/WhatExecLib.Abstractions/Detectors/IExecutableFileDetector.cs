/*
    WhatExecLib
    Copyright (c) 2025 Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.IO;

namespace AlastairLundy.WhatExecLib.Abstractions.Detectors;

/// <summary>
/// Provides an interface for detecting executable files based on their file type and system permissions.
/// </summary>
public interface IExecutableFileDetector
{
    /// <summary>
    /// Determines if a given file is executable.
    /// </summary>
    /// <param name="file">The file to be checked.</param>
    /// <returns>True if the file can be executed, false otherwise.</returns>
    bool IsFileExecutable(FileInfo file);
}
