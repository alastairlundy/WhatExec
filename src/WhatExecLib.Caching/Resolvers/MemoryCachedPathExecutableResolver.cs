/*
    WhatExecLib
    Copyright (c) 2025 Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using DotPrimitives.IO.Paths;

namespace WhatExecLib.Caching.Resolvers;

/// <summary>
///
/// </summary>
public class MemoryCachedPathExecutableResolver : PathExecutableResolver, ICachedPathExecutableResolver
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
    public MemoryCachedPathExecutableResolver(
        IMemoryCache cache)
    {
        _cache = cache;
    }

    public MemoryCachedPathExecutableResolver(
        IMemoryCache cache,
        TimeSpan defaultPathCacheLifespan,
        TimeSpan defaultPathExtensionsCacheLifespan)
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
            pathContents = PathEnvironmentVariable.GetDirectories();

            _cache.Set(PathCacheName, pathContents, pathCacheLifespan);
        }

        return pathContents;
    }

    protected string[] GetPathExtensions(TimeSpan pathExtensionsCacheLifespan)
    {
        string[]? pathContentsExtensions = _cache.Get<string[]>(PathExtensionCacheName);

        if (pathContentsExtensions is null)
        {
            pathContentsExtensions = PathEnvironmentVariable.GetPathFileExtensions();

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
    /// <returns></returns>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("android")]
    public KeyValuePair<string, FileInfo> ResolveExecutableFile(string inputFilePath,
        TimeSpan? pathCacheLifetime,
        TimeSpan? pathExtensionsCacheLifetime)
    {
        pathCacheLifetime ??= DefaultPathCacheLifespan;
        pathExtensionsCacheLifetime ??= DefaultPathExtensionsCacheLifespan;

        bool result = TryResolveExecutableFile(inputFilePath, pathCacheLifetime,
            pathExtensionsCacheLifetime, out KeyValuePair<string, FileInfo>? fileInfo);

        if (!result || fileInfo is null)
            throw new FileNotFoundException($"Could not find file: {inputFilePath}");

        if (!fileInfo.Value.Value.Exists)
            throw new FileNotFoundException($"Could not find file: {inputFilePath}");

        return new KeyValuePair<string, FileInfo>(inputFilePath, fileInfo.Value.Value);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="inputFilePath"></param>
    /// <param name="pathExtensionsCacheLifetime"></param>
    /// <param name="pathCacheLifetime"></param>
    /// <param name="resolvedExecutable"></param>
    /// <returns></returns>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("android")]
    public bool TryResolveExecutableFile(string inputFilePath, TimeSpan? pathExtensionsCacheLifetime,
        TimeSpan? pathCacheLifetime,
        out KeyValuePair<string, FileInfo>? resolvedExecutable)
    {
        bool success = TryResolveAllExecutableFiles(pathExtensionsCacheLifetime, pathCacheLifetime, out IReadOnlyDictionary<string, FileInfo> resolvedExecutables, inputFilePath);
        resolvedExecutable = resolvedExecutables.First(f => f.Key == inputFilePath);
        
        return success;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="pathExtensionsCacheLifetime"></param>
    /// <param name="pathCacheLifetime"></param>
    /// <param name="inputFilePaths"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("android")]
    public IReadOnlyDictionary<string, FileInfo> ResolveAllExecutableFiles(TimeSpan? pathExtensionsCacheLifetime, TimeSpan? pathCacheLifetime,
        params string[] inputFilePaths)
    {
        pathCacheLifetime ??= DefaultPathCacheLifespan;
        pathExtensionsCacheLifetime ??= DefaultPathExtensionsCacheLifespan;

        ArgumentNullException.ThrowIfNull(inputFilePaths);

        string[] pathExtensions = GetPathExtensions(pathExtensionsCacheLifetime.Value);
        string[] pathContents = GetPathContents(pathCacheLifetime.Value)
                                ?? throw new InvalidOperationException("PATH Variable could not be found.");
        
        return InternalResolveFilePaths(inputFilePaths, pathContents, pathExtensions);
    }

    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="pathExtensionsCacheLifetime"></param>
    /// <param name="pathCacheLifetime"></param>
    /// <param name="resolvedExecutables"></param>
    /// <param name="inputFilePaths"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("android")]
    public bool TryResolveAllExecutableFiles(TimeSpan? pathExtensionsCacheLifetime, TimeSpan? pathCacheLifetime,
        out IReadOnlyDictionary<string, FileInfo> resolvedExecutables, params string[] inputFilePaths)
    {
        pathCacheLifetime ??= DefaultPathCacheLifespan;
        pathExtensionsCacheLifetime ??= DefaultPathExtensionsCacheLifespan;

        ArgumentNullException.ThrowIfNull(inputFilePaths);

        string[] pathExtensions = GetPathExtensions(pathExtensionsCacheLifetime.Value);
        string[] pathContents = GetPathContents(pathCacheLifetime.Value)
                                ?? throw new InvalidOperationException("PATH Variable could not be found.");

        return InternalTryResolveFilePaths(inputFilePaths, out resolvedExecutables, pathContents, pathExtensions);
    }
}
