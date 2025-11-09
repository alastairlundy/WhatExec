using System;
using AlastairLundy.WhatExecLib;
using AlastairLundy.WhatExecLib.Abstractions;
using AlastairLundy.WhatExecLib.Abstractions.Detectors;
using Microsoft.Extensions.Caching.Memory;

namespace WhatExecLib.Caching.Resolvers;

/// <summary>
///
/// </summary>
public class CachedPathExecutableResolver : PathExecutableResolver, IPathExecutableResolver
{
    private readonly IMemoryCache _cache;

    private const string PathExtensionCacheName = "PathExtensionCacheData";
    private const string PathCacheName = "PathCacheData";

    private TimeSpan PathCacheLifespan { get; set; } = TimeSpan.FromMinutes(5);
    private TimeSpan PathExtensionsCacheLifespan { get; set; } = TimeSpan.FromHours(6);

    /// <summary>
    ///
    /// </summary>
    /// <param name="executableFileDetector"></param>
    /// <param name="cache"></param>
    /// <param name="pathCacheLifespan"></param>
    /// <param name="pathExtensionsCacheLifespan"></param>
    public CachedPathExecutableResolver(
        IExecutableFileDetector executableFileDetector,
        IMemoryCache cache,
        TimeSpan? pathCacheLifespan = null,
        TimeSpan? pathExtensionsCacheLifespan = null
    )
        : base(executableFileDetector)
    {
        _cache = cache;

        if (pathCacheLifespan.HasValue)
            PathCacheLifespan = pathCacheLifespan.Value;

        if (pathExtensionsCacheLifespan.HasValue)
            PathExtensionsCacheLifespan = pathExtensionsCacheLifespan.Value;
    }

    protected override string[] GetPathContents()
    {
        string[]? pathContents = _cache.Get<string[]>(PathCacheName);

        if (pathContents is null)
        {
            pathContents = base.GetPathContents();

            _cache.Set(PathCacheName, pathContents, PathCacheLifespan);
        }

        return pathContents;
    }

    protected override string[] GetPathExtensions()
    {
        string[]? pathContentsExtensions = _cache.Get<string[]>(PathExtensionCacheName);

        if (pathContentsExtensions is null)
        {
            pathContentsExtensions = base.GetPathExtensions();

            _cache.Set(PathExtensionCacheName, pathContentsExtensions, PathExtensionsCacheLifespan);
        }

        return pathContentsExtensions;
    }
}
