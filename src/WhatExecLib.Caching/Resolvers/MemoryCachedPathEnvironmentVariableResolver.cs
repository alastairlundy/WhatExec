/*
    WhatExecLib
    Copyright (c) 2025-2026 Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

namespace WhatExecLib.Caching.Resolvers;

/// <summary>
///
/// </summary>
public class MemoryCachedPathEnvironmentVariableResolver : PathEnvironmentVariableResolver, ICachedPathEnvironmentVariableResolver
{
    private readonly IMemoryCache _cache;

    private const string PathExtensionCacheName = "PathExtensionCacheData";
    private const string PathCacheName = "PathCacheData";

    private TimeSpan DefaultPathCacheLifespan { get; } = TimeSpan.FromMinutes(5);
    private TimeSpan DefaultPathExtensionsCacheLifespan { get; } = TimeSpan.FromMinutes(10);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="cache"></param>
    /// <param name="pathEnvironmentVariableDetector"></param>
    /// <param name="executableFileDetector"></param>
    public MemoryCachedPathEnvironmentVariableResolver(
        IMemoryCache cache, IPathEnvironmentVariableDetector pathEnvironmentVariableDetector, IExecutableFileDetector executableFileDetector) :
        base(pathEnvironmentVariableDetector, executableFileDetector)
    {
        _cache = cache;
    }

    public MemoryCachedPathEnvironmentVariableResolver(
        IMemoryCache cache,
        TimeSpan defaultPathCacheLifespan,
        TimeSpan defaultPathExtensionsCacheLifespan,
        IPathEnvironmentVariableDetector pathEnvironmentVariableDetector,
        IExecutableFileDetector executableFileDetector) : base(pathEnvironmentVariableDetector, executableFileDetector)
    {
        _cache = cache;
        DefaultPathCacheLifespan = defaultPathCacheLifespan;
        DefaultPathExtensionsCacheLifespan = defaultPathExtensionsCacheLifespan;
    }

    protected string[]? GetPathContents(TimeSpan pathCacheLifespan)
    {
        string[]? pathContents = _cache.Get<string[]>(PathCacheName);

        if (pathContents is null)
        {
            pathContents = base.GetPathContents();

            _cache.Set(PathCacheName, pathContents, pathCacheLifespan);
        }

        return pathContents;
    }

    protected string[] GetPathExtensions(TimeSpan pathExtensionsCacheLifespan)
    {
        string[]? pathContentsExtensions = _cache.Get<string[]>(PathExtensionCacheName);

        if (pathContentsExtensions is null)
        {
            pathContentsExtensions = base.GetPathExtensions();

            _cache.Set(PathExtensionCacheName, pathContentsExtensions, pathExtensionsCacheLifespan);
        }

        return pathContentsExtensions;
    }
    
    protected new string[]? GetPathContents() => 
        GetPathContents(DefaultPathCacheLifespan);
    
    protected new string[] GetPathExtensions() => 
        GetPathExtensions(DefaultPathExtensionsCacheLifespan);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="inputFilePath"></param>
    /// <param name="pathCacheLifetime"></param>
    /// <param name="pathExtensionsCacheLifetime"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("android")]
    public async Task<KeyValuePair<string, FileInfo>> ResolveExecutableFileAsync(string inputFilePath,
        TimeSpan? pathCacheLifetime,
        TimeSpan? pathExtensionsCacheLifetime, CancellationToken cancellationToken)
    {
        pathCacheLifetime ??= DefaultPathCacheLifespan;
        pathExtensionsCacheLifetime ??= DefaultPathExtensionsCacheLifespan;

        (bool success, KeyValuePair<string, FileInfo>? resolvedExecutable) result = await TryResolveExecutableFileAsync(inputFilePath, pathCacheLifetime,
            pathExtensionsCacheLifetime, cancellationToken);

        if (!result.success || result.resolvedExecutable is null)
            throw new FileNotFoundException($"Could not find file: {inputFilePath}");

        if (!result.resolvedExecutable.Value.Value.Exists)
            throw new FileNotFoundException($"Could not find file: {inputFilePath}");

        return new KeyValuePair<string, FileInfo>(inputFilePath, result.resolvedExecutable.Value.Value);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="inputFilePath"></param>
    /// <param name="pathExtensionsCacheLifetime"></param>
    /// <param name="pathCacheLifetime"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("android")]
    public async Task<(bool, KeyValuePair<string, FileInfo>?)> TryResolveExecutableFileAsync(string inputFilePath, TimeSpan? pathExtensionsCacheLifetime,
        TimeSpan? pathCacheLifetime,
        CancellationToken cancellationToken)
    {
        (bool success, IReadOnlyDictionary<string, FileInfo> resolvedExecutables) results =
            await TryResolveAllExecutableFilesAsync([inputFilePath], pathExtensionsCacheLifetime, pathCacheLifetime,
                cancellationToken);
        
        return (results.success,  results.resolvedExecutables.First(f => f.Key == inputFilePath));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="inputFilePaths"></param>
    /// <param name="pathExtensionsCacheLifetime"></param>
    /// <param name="pathCacheLifetime"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("android")]
    public async Task<IReadOnlyDictionary<string, FileInfo>> ResolveAllExecutableFilesAsync(string[] inputFilePaths,
        TimeSpan? pathExtensionsCacheLifetime, TimeSpan? pathCacheLifetime,
        CancellationToken cancellationToken)
    {
        pathCacheLifetime ??= DefaultPathCacheLifespan;
        pathExtensionsCacheLifetime ??= DefaultPathExtensionsCacheLifespan;

        ArgumentNullException.ThrowIfNull(inputFilePaths);

        string[] pathExtensions = GetPathExtensions(pathExtensionsCacheLifetime.Value);
        string[] pathContents = GetPathContents(pathCacheLifetime.Value)
                                ?? throw new InvalidOperationException("PATH Variable could not be found.");
        
        return await InternalResolveFilePaths(inputFilePaths, pathContents, pathExtensions, cancellationToken);
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="pathExtensionsCacheLifetime"></param>
    /// <param name="pathCacheLifetime"></param>
    /// <param name="inputFilePaths"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("android")]
    public async Task<(bool, IReadOnlyDictionary<string, FileInfo>)> TryResolveAllExecutableFilesAsync(string[] inputFilePaths, TimeSpan? pathExtensionsCacheLifetime,
        TimeSpan? pathCacheLifetime,
        CancellationToken cancellationToken)
    {
        pathCacheLifetime ??= DefaultPathCacheLifespan;
        pathExtensionsCacheLifetime ??= DefaultPathExtensionsCacheLifespan;

        ArgumentNullException.ThrowIfNull(inputFilePaths);

        string[] pathExtensions = GetPathExtensions(pathExtensionsCacheLifetime.Value);
        string[] pathContents = GetPathContents(pathCacheLifetime.Value)
                                ?? throw new InvalidOperationException("PATH Variable could not be found.");

        return await InternalTryResolveFilePathsAsync(inputFilePaths, pathContents, pathExtensions, cancellationToken);
    }
}
