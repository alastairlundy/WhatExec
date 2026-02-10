/*
    WhatExecLib
    Copyright (c) 2025-2026 Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

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
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<KeyValuePair<string, FileInfo>> ResolveExecutableFileAsync(string inputFilePath,
        TimeSpan? pathExtensionsCacheLifetime,
        TimeSpan? pathCacheLifetime,
        CancellationToken cancellationToken);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="inputFilePath"></param>
    /// <param name="pathExtensionsCacheLifetime"></param>
    /// <param name="pathCacheLifetime"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<(bool, KeyValuePair<string, FileInfo>?)> TryResolveExecutableFileAsync(string inputFilePath,
        TimeSpan? pathExtensionsCacheLifetime,
        TimeSpan? pathCacheLifetime,
        CancellationToken cancellationToken);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="inputFilePaths"></param>
    /// <param name="pathExtensionsCacheLifetime"></param>
    /// <param name="pathCacheLifetime"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<IReadOnlyDictionary<string, FileInfo>> ResolveAllExecutableFilesAsync(string[] inputFilePaths,
        TimeSpan? pathExtensionsCacheLifetime,
        TimeSpan? pathCacheLifetime,
        CancellationToken cancellationToken);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="pathExtensionsCacheLifetime"></param>
    /// <param name="pathCacheLifetime"></param>
    /// <param name="inputFilePaths"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<(bool, IReadOnlyDictionary<string, FileInfo>)> TryResolveAllExecutableFilesAsync(string[] inputFilePaths, 
        TimeSpan? pathExtensionsCacheLifetime,
        TimeSpan? pathCacheLifetime,
        CancellationToken cancellationToken); 
}
