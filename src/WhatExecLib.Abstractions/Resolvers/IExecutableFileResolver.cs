/*
    WhatExecLib
    Copyright (c) 2025-2026 Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

namespace WhatExec.Lib.Abstractions;

/// <summary>
/// Represents an interface for locating executable files based on search criteria.
/// </summary>
public interface IExecutableFileResolver
{
    /// <summary>
    /// An event that is triggered each time an executable file was located during the resolution process along with the input file name the <see cref="FileInfo"/> belongs to.
    /// </summary>
    event EventHandler<KeyValuePair<string, FileInfo>> ExecutableFileLocated;

    /// <summary>
    /// Locates an executable file within a specified drive based on the given search criteria.
    /// </summary>
    /// <param name="executableName">The name of the executable file to locate.</param>
    /// <param name="searchOption">Specifies whether to search all directories or only the top-level directory.</param>
    /// <returns>A <see cref="FileInfo"/> representing the located executable file if found.</returns>
    FileInfo LocateExecutable(string executableName, SearchOption searchOption);

    /// <summary>
    /// Locates an executable file within a specified drive based on the given search criteria.
    /// </summary>
    /// <param name="executableFileName">The name of the executable file to locate.</param>
    /// <param name="directorySearchOption">Specifies whether to search all directories or only the top-level directory.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>A <see cref="FileInfo"/> representing the located executable file if found.</returns>
    Task<FileInfo> LocateExecutableAsync(string executableFileName, SearchOption directorySearchOption, CancellationToken cancellationToken);

    /// <summary>
    /// Asynchronously locates an executable file within specified drives based on the given search criteria.
    /// </summary>
    /// <param name="executableFileName">The name of the executable file to locate.</param>
    /// <param name="directorySearchOption">Specifies whether to search all directories or only the top-level directory.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A tuple containing a boolean indicating if the executable was found and its location as a FileInfo object if successful, otherwise null.</returns>
    Task<(bool, FileInfo?)> TryLocateExecutableAsync(string executableFileName, SearchOption directorySearchOption,
        CancellationToken cancellationToken);

    /// <summary>
    /// Attempts to locate an executable file within a specified drive based on the given search criteria.
    /// </summary>
    /// <param name="executableFileName">The name of the executable file to locate.</param>
    /// <param name="directorySearchOption">Specifies whether to search all directories or only the top-level directory.</param>
    /// <returns>A tuple indicating if the file was found and its associated <see cref="FileInfo"/> representing the located executable file if found, otherwise null.</returns>
    (bool, FileInfo?) TryLocateExecutable(string executableFileName, SearchOption directorySearchOption);

    /// <summary>
    /// Locates the specified executable files and returns a dictionary containing their names and corresponding file information.
    /// </summary>
    /// <param name="executableFileNames">An array of strings representing the names of the executable files to locate.</param>
    /// <param name="directorySearchOption"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>A read-only dictionary where the keys are the names of the executable files and the values are their corresponding <see cref="FileInfo"/> objects.</returns>
    /// <exception cref="FileNotFoundException">Thrown if one or more of the specified executable files was not found.</exception>
    Task<IReadOnlyDictionary<string, FileInfo>> LocateExecutableFilesAsync(string[] executableFileNames, SearchOption directorySearchOption, CancellationToken cancellationToken);

    /// <summary>
    /// Locates multiple executable files within specified directories based on an array of executable names.
    /// </summary>
    /// <param name="executableFileNames">An array of names of the executable files to locate.</param>
    /// <param name="directorySearchOption">Specifies whether to search all directories or only the top-level directory.</param>
    /// <returns>A dictionary where keys are executable file names and values are corresponding <see cref="FileInfo"/> objects representing the located executables if found.</returns>
    IReadOnlyDictionary<string, FileInfo> LocateExecutableFiles(string[] executableFileNames,
        SearchOption directorySearchOption);

    /// <summary>
    /// Attempts to locate the specified executable files and returns a boolean indicating the success of the operation.
    /// </summary>
    /// <param name="executableFileNames">An array of strings representing the names of the executable files to locate.</param>
    /// <param name="directorySearchOption"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>
    /// A boolean value indicating whether all the specified executable files were successfully located.
    /// </returns>
    Task<(bool, IReadOnlyDictionary<string, FileInfo>)> TryLocateExecutableFilesAsync(string[] executableFileNames, SearchOption directorySearchOption,
        CancellationToken cancellationToken);

    /// <summary>
    /// Attempts to locate multiple executable files within a specified drive based on an array of their names and search criteria.
    /// </summary>
    /// <param name="executableFileNames">An array of names of the executable files to locate.</param>
    /// <param name="directorySearchOption">Specifies whether to search all directories or only the top-level directory.</param>
    /// <returns>A tuple indicating success (true) and a dictionary containing the located executable files if successful, otherwise false and an empty dictionary.</returns>
    (bool, IReadOnlyDictionary<string, FileInfo>) TryLocateExecutableFiles(string[] executableFileNames,
        SearchOption directorySearchOption);
}
