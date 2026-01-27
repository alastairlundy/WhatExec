/*
    WhatExecLib
    Copyright (c) 2025 Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace WhatExecLib.Caching.Extensions;

/// <summary>
///
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <param name="services">The <see cref="IServiceCollection"/> to which the caching services will be added.</param>
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Configures caching services for WhatExecLib by adding support for memory caching and cached path executable resolution.
        /// The type of caching services added is determined by the specified service lifetime.
        /// </summary>
        /// <param name="serviceLifetime">The <see cref="ServiceLifetime"/> specifying the lifecycle of the caching services.</param>
        /// <param name="pathCacheLifespan">An optional <see cref="TimeSpan"/> specifying the lifespan of path cache entries. If null, the default behavior is applied.</param>
        /// <param name="pathExtensionsCacheLifespan">An optional <see cref="TimeSpan"/> specifying the lifespan of path extension cache entries. If null, the default behavior is applied.</param>
        /// <returns>The modified <see cref="IServiceCollection"/> with caching services configured.</returns>
        public IServiceCollection AddWhatExecLibCaching(
            ServiceLifetime serviceLifetime,
            TimeSpan? pathCacheLifespan = null,
            TimeSpan? pathExtensionsCacheLifespan = null
        )
        {
            services.AddMemoryCache();

            if (pathCacheLifespan is null || pathExtensionsCacheLifespan is null)
            {
                switch (serviceLifetime)
                {
                    case ServiceLifetime.Scoped:
                        services.AddScoped<
                            ICachedPathEnvironmentVariableResolver,
                            MemoryCachedPathEnvironmentVariableResolver
                        >();
                        break;
                    case ServiceLifetime.Singleton:
                        services.AddSingleton<
                            ICachedPathEnvironmentVariableResolver,
                            MemoryCachedPathEnvironmentVariableResolver
                        >();
                        break;
                    case ServiceLifetime.Transient:
                        services.AddTransient<
                            ICachedPathEnvironmentVariableResolver,
                            MemoryCachedPathEnvironmentVariableResolver
                        >();
                        break;
                }
            }
            else
            {
                switch (serviceLifetime)
                {
                    case ServiceLifetime.Scoped:
                        services.AddScoped<ICachedPathEnvironmentVariableResolver>(
                            sp => new MemoryCachedPathEnvironmentVariableResolver(
                                sp.GetRequiredService<IMemoryCache>(),
                                (TimeSpan)pathCacheLifespan,
                                (TimeSpan)pathExtensionsCacheLifespan
                            )
                        );
                        break;
                    case ServiceLifetime.Singleton:
                        services.AddSingleton<ICachedPathEnvironmentVariableResolver>(
                            sp => new MemoryCachedPathEnvironmentVariableResolver(
                                sp.GetRequiredService<IMemoryCache>(),
                                (TimeSpan)pathCacheLifespan,
                                (TimeSpan)pathExtensionsCacheLifespan
                            )
                        );
                        ;
                        break;
                    case ServiceLifetime.Transient:
                        services.AddTransient<ICachedPathEnvironmentVariableResolver>(
                            sp => new MemoryCachedPathEnvironmentVariableResolver(
                                sp.GetRequiredService<IMemoryCache>(),
                                (TimeSpan)pathCacheLifespan,
                                (TimeSpan)pathExtensionsCacheLifespan
                            )
                        );
                        ;
                        break;
                }
            }
            return services;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="serviceLifetime"></param>
        /// <param name="pathCacheLifespan"></param>
        /// <param name="pathExtensionsCacheLifespan"></param>
        /// <returns></returns>
        public IServiceCollection RedirectNonCachedWhatExecLib(
            ServiceLifetime serviceLifetime,
            TimeSpan? pathCacheLifespan = null,
            TimeSpan? pathExtensionsCacheLifespan = null
        )
        {
            services.AddMemoryCache();

            if (pathCacheLifespan is null || pathExtensionsCacheLifespan is null)
            {
                switch (serviceLifetime)
                {
                    case ServiceLifetime.Scoped:
                        services.TryAddScoped<
                            IPathEnvironmentVariableResolver,
                            MemoryCachedPathEnvironmentVariableResolver
                        >();
                        break;
                    case ServiceLifetime.Singleton:
                        services.TryAddSingleton<
                            IPathEnvironmentVariableResolver,
                            MemoryCachedPathEnvironmentVariableResolver
                        >();
                        break;
                    case ServiceLifetime.Transient:
                        services.TryAddTransient<
                            IPathEnvironmentVariableResolver,
                            MemoryCachedPathEnvironmentVariableResolver
                        >();
                        break;
                }
            }
            else
            {
                switch (serviceLifetime)
                {
                    case ServiceLifetime.Scoped:
                        services.TryAddScoped<IPathEnvironmentVariableResolver>(
                            sp => new MemoryCachedPathEnvironmentVariableResolver(
                                sp.GetRequiredService<IMemoryCache>(),
                                (TimeSpan)pathCacheLifespan,
                                (TimeSpan)pathExtensionsCacheLifespan
                            )
                        );
                        break;
                    case ServiceLifetime.Singleton:
                        services.TryAddSingleton<IPathEnvironmentVariableResolver>(
                            sp => new MemoryCachedPathEnvironmentVariableResolver(
                                sp.GetRequiredService<IMemoryCache>(),
                                (TimeSpan)pathCacheLifespan,
                                (TimeSpan)pathExtensionsCacheLifespan
                            )
                        );
                        ;
                        break;
                    case ServiceLifetime.Transient:
                        services.TryAddTransient<IPathEnvironmentVariableResolver>(
                            sp => new MemoryCachedPathEnvironmentVariableResolver(
                                sp.GetRequiredService<IMemoryCache>(),
                                (TimeSpan)pathCacheLifespan,
                                (TimeSpan)pathExtensionsCacheLifespan
                            )
                        );
                        ;
                        break;
                }
            }
            return services;
        }
    }
}
