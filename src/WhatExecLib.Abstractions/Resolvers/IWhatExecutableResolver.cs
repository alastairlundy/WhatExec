/*
    WhatExecLib
    Copyright (c) 2025 Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.IO;

namespace AlastairLundy.WhatExecLib.Abstractions;

/// <summary>
/// Defines methods for resolving the full file path of an executable based on an input path or file name.
/// </summary>
public interface IWhatExecutableResolver
{
    /// <summary>
    /// Resolves the full file path to an executable based on the provided input path.
    /// </summary>
    /// <param name="inputFilePath">The path or name of the file to resolve. This can be a full path, a relative path, or just the file name.</param>
    /// <returns>The resolved <see cref="FileInfo"/> object representing the full path to the file.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the file could not be resolved or located.</exception>
    FileInfo ResolveExecutableFilePath(string inputFilePath);

    /// <summary>
    /// Attempts to resolve the full file path to an executable based on the provided input path.
    /// </summary>
    /// <param name="inputFilePath">The path or name of the file to resolve. This can be a full path, a relative path, or just the file name.</param>
    /// <param name="fileInfo">When the method returns, contains the resolved <see cref="FileInfo"/> object representing the file's full path, if the resolution was successful. Otherwise, it is null.</param>
    /// <returns>True if the file path was resolved successfully; otherwise, false.</returns>
    bool TryResolveExecutableFilePath(string inputFilePath, out FileInfo? fileInfo);
}