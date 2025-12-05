/*
    WhatExecLib
    Copyright (c) 2025 Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Threading;
using System.Threading.Tasks;

namespace AlastairLundy.WhatExecLib.Abstractions.Locators;

public interface IExecutableFileLocator
{
    /// <summary>
    ///
    /// </summary>
    /// <param name="drive"></param>
    /// <param name="executableFileName"></param>
    /// <param name="directorySearchOption"></param>
    /// <returns></returns>
    FileInfo? LocateExecutableInDrive(
        DriveInfo drive,
        string executableFileName,
        SearchOption directorySearchOption
    );

    /// <summary>
    ///
    /// </summary>
    /// <param name="drive"></param>
    /// <param name="executableFileName"></param>
    /// <param name="directorySearchOption"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<FileInfo?> LocateExecutableInDriveAsync(
        DriveInfo drive,
        string executableFileName,
        SearchOption directorySearchOption,
        CancellationToken cancellationToken
    );

    /// <summary>
    ///
    /// </summary>
    /// <param name="directory"></param>
    /// <param name="executableFileName"></param>
    /// <param name="directorySearchOption"></param>
    /// <returns></returns>
    FileInfo? LocateExecutableInDirectory(
        DirectoryInfo directory,
        string executableFileName,
        SearchOption directorySearchOption
    );

    Task<FileInfo?> LocateExecutableInDirectoryAsync(
        DirectoryInfo directory,
        string executableFileName,
        SearchOption directorySearchOption,
        CancellationToken cancellationToken
    );

    /// <summary>
    ///
    /// </summary>
    /// <param name="executableFileName"></param>
    /// <param name="directorySearchOption"></param>
    /// <returns></returns>
    FileInfo? LocateExecutable(string executableFileName, SearchOption directorySearchOption);

    Task<FileInfo?> LocateExecutableAsync(
        string executableFileName,
        SearchOption directorySearchOption,
        CancellationToken cancellationToken
    );
}
