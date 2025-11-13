/*
    WhatExecLib
    Copyright (c) 2025 Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.IO;

namespace AlastairLundy.WhatExecLib.Abstractions.Locators;

public interface IExecutableFileLocator
{
    /// <summary>
    ///
    /// </summary>
    /// <param name="executableFileName"></param>
    /// <returns></returns>
    FileInfo? LocateExecutableInDrive(string executableFileName);

    /// <summary>
    ///
    /// </summary>
    /// <param name="executableDirectory"></param>
    /// <param name="directorySearchOption"></param>
    /// <returns></returns>
    FileInfo? LocateExecutableInDirectory(
        string executableDirectory,
        SearchOption directorySearchOption
    );

    /// <summary>
    ///
    /// </summary>
    /// <param name="executableFileName"></param>
    /// <returns></returns>
    FileInfo? LocateExecutable(string executableFileName);
}
