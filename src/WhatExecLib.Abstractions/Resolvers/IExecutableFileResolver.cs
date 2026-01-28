/*
    WhatExecLib
    Copyright (c) 2025-2026 Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

namespace WhatExecLib.Abstractions;

/// <summary>
/// 
/// </summary>
public interface IExecutableFileResolver
{
    /// <summary>
    /// Locates an executable file within a specified drive based on the given search criteria.
    /// </summary>
    /// <param name="executableFileName">The name of the executable file to locate.</param>
    /// <param name="directorySearchOption">Specifies whether to search all directories or only the top-level directory.</param>
    /// <returns>A <see cref="FileInfo"/> representing the located executable file if found; otherwise, null.</returns>
    FileInfo? LocateExecutable(string executableFileName, SearchOption directorySearchOption);
    
    /// <summary>
    /// Locates the specified executable files and returns a dictionary containing their names and corresponding file information.
    /// </summary>
    /// <param name="executableFileNames">An array of strings representing the names of the executable files to locate.</param>
    /// <returns>A read-only dictionary where the keys are the names of the executable files and the values are their corresponding <see cref="FileInfo"/> objects.</returns>
    /// <exception cref="FileNotFoundException">Thrown if one or more of the specified executable files was not found.</exception>
    IReadOnlyDictionary<string, FileInfo> LocateExecutableFiles(params string[] executableFileNames);

    /// <summary>
    /// Attempts to locate the specified executable files and returns a boolean indicating the success of the operation.
    /// </summary>
    /// <param name="executableFiles">An output parameter that, on success, contains a read-only dictionary mapping the names of the executable files to their corresponding <see cref="FileInfo"/> objects.</param>
    /// <param name="executableFileNames">An array of strings representing the names of the executable files to locate.</param>
    /// <returns>
    /// A boolean value indicating whether at least one of the specified executable files was successfully located.
    /// </returns>
    bool TryLocateExecutableFiles(out IReadOnlyDictionary<string, FileInfo> executableFiles,
        params string[] executableFileNames);
}
