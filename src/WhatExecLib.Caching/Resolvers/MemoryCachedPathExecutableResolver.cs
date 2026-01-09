/*
    WhatExecLib
    Copyright (c) 2025 Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

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
    /// <param name="executableFileDetector"></param>
    /// <param name="cache"></param>
    public MemoryCachedPathExecutableResolver(
        IExecutableFileDetector executableFileDetector,
        IMemoryCache cache
    )
        : base()
    {
        _cache = cache;
    }

    public MemoryCachedPathExecutableResolver(
        IExecutableFileDetector executableFileDetector,
        IMemoryCache cache,
        TimeSpan defaultPathCacheLifespan,
        TimeSpan defaultPathExtensionsCacheLifespan
    )
        : base()
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
    public new FileInfo GetResolvedExecutable(string inputFilePath) =>
        ResolveExecutableFile(
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
    public new bool TryResolveExecutable(string inputFilePath, out FileInfo? fileInfo) =>
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
    /// <param name="pathExtensionsCacheLifetime"></param>
    /// <param name="pathCacheLifetime"></param>
    /// <returns></returns>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("android")]
    public FileInfo ResolveExecutableFile(
        string inputFilePath,
        TimeSpan? pathCacheLifetime,
        TimeSpan? pathExtensionsCacheLifetime
    )
    {
        pathCacheLifetime ??= DefaultPathCacheLifespan;
        pathExtensionsCacheLifetime ??= DefaultPathExtensionsCacheLifespan;

        bool result = TryResolveExecutableFile(
            inputFilePath,
            pathCacheLifetime,
            pathExtensionsCacheLifetime,
            out FileInfo? fileInfo
        );

        if (result == false || fileInfo is null)
            throw new FileNotFoundException($"Could not find file: {inputFilePath}");

        if (File.Exists(fileInfo.FullName) == false)
            throw new FileNotFoundException($"Could not find file: {inputFilePath}");

        return fileInfo;
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="inputFilePath"></param>
    /// <param name="pathExtensionsCacheLifetime"></param>
    /// <param name="pathCacheLifetime"></param>
    /// <param name="fileInfo"></param>
    /// <returns></returns>
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("freebsd")]
    [SupportedOSPlatform("android")]
    public bool TryResolveExecutableFile(
        string inputFilePath,
        TimeSpan? pathCacheLifetime,
        TimeSpan? pathExtensionsCacheLifetime,
        out FileInfo? fileInfo
    )
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
                    fileInfo = info;
                    return true;
                }
            }

            fileInfo = null;
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
            fileInfo = null;
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
                    out fileInfo,
                    pathEntry,
                    pathExtension
                );

                if (result)
                    return result;
            }
        }

        fileInfo = null;
        return false;
    }
}
