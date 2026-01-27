/*
    WhatExecLib
    Copyright (c) 2025-2026 Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

namespace WhatExecLib.Abstractions.Locators;

public interface IExecutableFileLocator
{
    /// <summary>
    /// Locates an executable file within the specified drive based on the given search criteria.
    /// </summary>
    /// <param name="drive">The drive to search within.</param>
    /// <param name="executableFileName">The name of the executable file to locate.</param>
    /// <param name="directorySearchOption">Specifies whether to search all directories or only the top-level directory.</param>
    /// <returns>A <see cref="FileInfo"/> representing the located executable file if found; otherwise, null.</returns>
    FileInfo? LocateExecutableInDrive(
        DriveInfo drive,
        string executableFileName,
        SearchOption directorySearchOption
    );

    /// <summary>
    /// Locates an executable file within the specified directory that matches the given file name and search option criteria.
    /// </summary>
    /// <param name="directory">The directory to search within.</param>
    /// <param name="executableFileName">The name of the executable file to locate.</param>
    /// <param name="directorySearchOption">Specifies whether to search all subdirectories or only the top-level directory.</param>
    /// <returns>A <see cref="FileInfo"/> representing the located executable file if found; otherwise, null.</returns>
    FileInfo? LocateExecutableInDirectory(
        DirectoryInfo directory,
        string executableFileName,
        SearchOption directorySearchOption
    );

    /// <summary>
    ///
    /// </summary>
    /// <param name="executableFileName"></param>
    /// <param name="directorySearchOption"></param>
    /// <returns></returns>
    FileInfo? LocateExecutable(string executableFileName, SearchOption directorySearchOption);
}
