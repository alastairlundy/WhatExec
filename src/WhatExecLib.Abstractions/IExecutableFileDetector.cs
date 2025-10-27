/*
    XpWhereLib
    Copyright (c) 2024-2025 Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.IO;

namespace WhatExecLib.Abstractions;

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

    /// <summary>
    /// Determines whether a specified file has executable permissions.
    /// </summary>
    /// <param name="file">The file to be checked.</param>
    /// <returns>True if the file has execute permissions, false otherwise.</returns>
    bool DoesFileHaveExecutablePermissions(FileInfo file);
    
    /// <summary>
    /// Determines whether a specified file has a valid executable file extension.
    /// </summary>
    /// <param name="file">The file to be checked.</param>
    /// <returns>True if the file extension is valid for an executable, false otherwise.</returns>
    bool DoesFileHaveExecutableExtension(FileInfo file);
}