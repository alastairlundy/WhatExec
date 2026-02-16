/*
    WhatExecLib
    Copyright (c) 2025-2026 Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

namespace WhatExec.Lib.Abstractions;

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
    /// <param name="cancellationToken"></param>
    /// <returns>A <see cref="FileInfo"/> representing the located executable file if found.</returns>
    Task<FileInfo> LocateExecutableAsync(string executableFileName, SearchOption directorySearchOption, CancellationToken cancellationToken);
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="executableFileName"></param>
    /// <param name="directorySearchOption"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<(bool, FileInfo?)> TryLocateExecutable(string executableFileName, SearchOption directorySearchOption, CancellationToken cancellationToken);

    /// <summary>
    /// Locates the specified executable files and returns a dictionary containing their names and corresponding file information.
    /// </summary>
    /// <param name="executableFileNames">An array of strings representing the names of the executable files to locate.</param>
    /// <param name="directorySearchOption"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>A read-only dictionary where the keys are the names of the executable files and the values are their corresponding <see cref="FileInfo"/> objects.</returns>
    /// <exception cref="FileNotFoundException">Thrown if one or more of the specified executable files was not found.</exception>
    Task<IReadOnlyDictionary<string, FileInfo>> LocateExecutableFiles(string[] executableFileNames, SearchOption directorySearchOption, CancellationToken cancellationToken);

    /// <summary>
    /// Attempts to locate the specified executable files and returns a boolean indicating the success of the operation.
    /// </summary>
    /// <param name="executableFileNames">An array of strings representing the names of the executable files to locate.</param>
    /// <param name="directorySearchOption"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>
    /// A boolean value indicating whether at least one of the specified executable files was successfully located.
    /// </returns>
    Task<(bool, IReadOnlyDictionary<string, FileInfo>)> TryLocateExecutableFilesAsync(string[] executableFileNames, SearchOption directorySearchOption,
        CancellationToken cancellationToken);
}
