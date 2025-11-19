/*
    WhatExecLib
    Copyright (c) 2025 Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.IO;
using AlastairLundy.WhatExecLib.Abstractions;

namespace AlastairLundy.WhatExecLib.Caching;

/// <summary>
///
/// </summary>
public interface ICachedPathExecutableResolver : IPathExecutableResolver
{
    /// <summary>
    ///
    /// </summary>
    /// <param name="inputFilePath"></param>
    /// <param name="pathExtensionsCacheLifetime"></param>
    /// <param name="pathCacheLifetime"></param>
    /// <returns></returns>
    FileInfo ResolvePathEnvironmentExecutableFile(
        string inputFilePath,
        TimeSpan? pathExtensionsCacheLifetime,
        TimeSpan? pathCacheLifetime
    );

    /// <summary>
    ///
    /// </summary>
    /// <param name="inputFilePath"></param>
    /// <param name="pathExtensionsCacheLifetime"></param>
    /// <param name="pathCacheLifetime"></param>
    /// <param name="fileInfo"></param>
    /// <returns></returns>
    bool TryResolvePathEnvironmentExecutableFile(
        string inputFilePath,
        TimeSpan? pathExtensionsCacheLifetime,
        TimeSpan? pathCacheLifetime,
        out FileInfo? fileInfo
    );
}
