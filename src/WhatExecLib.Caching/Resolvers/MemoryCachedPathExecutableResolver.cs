/*
    WhatExecLib
    Copyright (c) 2025 Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Collections.Generic;
using System.IO;
using System.Runtime.Versioning;
using DotPrimitives.IO.Paths;

namespace WhatExecLib.Caching.Resolvers;

/// <summary>
///
/// </summary>
public class MemoryCachedPathExecutableResolver
    : PathExecutableResolver,
        ICachedPathExecutableResolver
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
        IMemoryCache cache
    )
    {
        _cache = cache;
    }

    public MemoryCachedPathExecutableResolver(
        IMemoryCache cache,
        TimeSpan defaultPathCacheLifespan,
        TimeSpan defaultPathExtensionsCacheLifespan
    )
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

    /// <summary>
    ///
    /// </summary>
    /// <param name="inputFilePath"></param>
    /// <returns></returns>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("android")]
    public new KeyValuePair<string, FileInfo> ResolveExecutableFilePath(string inputFilePath)
        => ResolveExecutableFile(
            inputFilePath,
            DefaultPathCacheLifespan,
            DefaultPathExtensionsCacheLifespan
        );

    /// <summary>
    ///
    /// </summary>
    /// <param name="inputFilePath"></param>
    /// <param name="fileInfo"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("android")]
    public new bool TryResolveExecutable(string inputFilePath, out KeyValuePair<string, FileInfo>? fileInfo) =>
        TryResolveExecutableFile(
            inputFilePath,
            DefaultPathCacheLifespan,
            DefaultPathExtensionsCacheLifespan,
            out fileInfo
        );

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

        bool result = TryResolveExecutableFile(
            inputFilePath,
            pathCacheLifetime,
            pathExtensionsCacheLifetime,
            out KeyValuePair<string, FileInfo>? fileInfo
        );

        if (result == false || fileInfo is null)
            throw new FileNotFoundException($"Could not find file: {inputFilePath}");

        if (fileInfo.Value.Value.Exists == false)
            throw new FileNotFoundException($"Could not find file: {inputFilePath}");

        return new KeyValuePair<string, FileInfo>(inputFilePath, fileInfo.Value.Value);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="inputFilePath"></param>
    /// <param name="pathCacheLifetime"></param>
    /// <param name="pathExtensionsCacheLifetime"></param>
    /// <param name="resolvedExecutables"></param>
    /// <returns></returns>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("android")]
    public bool TryResolveExecutableFile(string inputFilePath,
        TimeSpan? pathCacheLifetime,
        TimeSpan? pathExtensionsCacheLifetime,
        out KeyValuePair<string, FileInfo>? resolvedExecutables)
    {
        ArgumentException.ThrowIfNullOrEmpty(inputFilePath);

        pathCacheLifetime ??= DefaultPathCacheLifespan;
        pathExtensionsCacheLifetime ??= DefaultPathExtensionsCacheLifespan;

        if (
            Path.IsPathRooted(inputFilePath)
            || inputFilePath.Contains(Path.DirectorySeparatorChar)
            || inputFilePath.Contains(Path.AltDirectorySeparatorChar)
        )
        {
            if (File.Exists(inputFilePath))
            {
                if (ExecutableFileIsValid(inputFilePath, out FileInfo? info) && info is not null)
                {
                    resolvedExecutables = new KeyValuePair<string, FileInfo>(inputFilePath, info);
                    return true;
                }
            }

            resolvedExecutables = null;
            return false;
        }

        bool fileHasExtension = Path.GetExtension(inputFilePath) != string.Empty;

        string[] pathExtensions = GetPathExtensions((TimeSpan)pathExtensionsCacheLifetime);
        string[] pathContents;

        try
        {
            pathContents =
                GetPathContents((TimeSpan)pathCacheLifetime)
                ?? throw new InvalidOperationException("PATH Variable could not be found.");
        }
        catch (InvalidOperationException)
        {
            resolvedExecutables = null;
            return false;
        }

        foreach (string pathEntry in pathContents)
        {
            if (!fileHasExtension)
            {
                pathExtensions = [""];
            }

            foreach (string pathExtension in pathExtensions)
            {
                bool result = CheckFileExists(
                    inputFilePath,
                    out FileInfo? fileInfo,
                    pathEntry,
                    pathExtension
                );

                if (result && fileInfo is not null)
                {
                    resolvedExecutables = new KeyValuePair<string,FileInfo>(inputFilePath, fileInfo);
                    return true;
                }
            }
        }

        resolvedExecutables = null;
        return false;
    }
}
