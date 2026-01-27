/*
    WhatExecLib
    Copyright (c) 2025-2026 Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Collections.Generic;
using System.IO;

namespace WhatExecLib.Caching;

/// <summary>
///
/// </summary>
public interface ICachedPathEnvironmentVariableResolver : IPathEnvironmentVariableResolver
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="inputFilePath"></param>
    /// <param name="pathExtensionsCacheLifetime"></param>
    /// <param name="pathCacheLifetime"></param>
    /// <returns></returns>
    KeyValuePair<string, FileInfo> ResolveExecutableFile(
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
    /// <param name="resolvedExecutable"></param>
    /// <returns></returns>
    bool TryResolveExecutableFile(
        string inputFilePath,
        TimeSpan? pathExtensionsCacheLifetime,
        TimeSpan? pathCacheLifetime,
        out KeyValuePair<string, FileInfo>? resolvedExecutable
    );

    /// <summary>
    /// 
    /// </summary>
    /// <param name="pathExtensionsCacheLifetime"></param>
    /// <param name="pathCacheLifetime"></param>
    /// <param name="inputFilePaths"></param>
    /// <returns></returns>
    IReadOnlyDictionary<string, FileInfo> ResolveAllExecutableFiles(
        TimeSpan? pathExtensionsCacheLifetime,
        TimeSpan? pathCacheLifetime,
        params string[] inputFilePaths
    );

    /// <summary>
    /// 
    /// </summary>
    /// <param name="pathExtensionsCacheLifetime"></param>
    /// <param name="pathCacheLifetime"></param>
    /// <param name="resolvedExecutables"></param>
    /// <param name="inputFilePaths"></param>
    /// <returns></returns>
    bool TryResolveAllExecutableFiles(TimeSpan? pathExtensionsCacheLifetime,
        TimeSpan? pathCacheLifetime,
        out IReadOnlyDictionary<string, FileInfo> resolvedExecutables,
        params string[] inputFilePaths); 
}
