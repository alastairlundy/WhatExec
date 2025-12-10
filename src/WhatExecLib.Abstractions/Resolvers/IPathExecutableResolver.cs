/*
    WhatExecLib
    Copyright (c) 2025 Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

namespace WhatExecLib.Abstractions;

/// <summary>
/// Defines methods to resolve file paths for executable files based on the system's PATH environment variable.
/// </summary>
public interface IPathExecutableResolver
{
    /// <summary>
    /// Resolves the file path of a file name that is in the PATH Environment Variable.
    /// </summary>
    /// <param name="inputFilePath"></param>
    /// <returns></returns>
    FileInfo ResolveExecutableFile(string inputFilePath);

    /// <summary>
    ///
    /// </summary>
    /// <param name="inputFilePaths"></param>
    /// <returns></returns>
    FileInfo[] ResolveExecutableFiles(params string[] inputFilePaths);

    /// <summary>
    /// Attempts to resolve a file from the system's PATH environment variable using the provided file name.
    /// </summary>
    /// <param name="inputFilePath">The name of the file to resolve, including optional relative or absolute paths.</param>
    /// <param name="fileInfo">When this method returns, contains the resolved <see cref="FileInfo"/> object if the resolution is successful; otherwise, null.</param>
    /// <returns>True if the file is successfully resolved; otherwise, false.</returns>
    bool TryResolveExecutableFile(string inputFilePath, out FileInfo? fileInfo);

    /// <summary>
    ///
    /// </summary>
    /// <param name="inputFilePaths"></param>
    /// <param name="fileInfos"></param>
    /// <returns></returns>
    bool TryResolveExecutableFiles(string[] inputFilePaths, out FileInfo[]? fileInfos);
}
