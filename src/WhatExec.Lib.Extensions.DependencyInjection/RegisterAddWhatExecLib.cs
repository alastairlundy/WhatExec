/*
    WhatExec.Lib
    Copyright (c) 2025-2026 Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using Microsoft.Extensions.DependencyInjection.Extensions;

namespace WhatExec.Lib.Extensions.DependencyInjection;

/// <summary>
/// Provides extension methods for registering and adding WhatExec.Lib functionality to the dependency injection container.
/// </summary>
public static class RegisterAddWhatExecLib
{
    /// <param name="services"></param>
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds the AddWhatExecLib extension methods to IServiceCollection.
        /// </summary>
        /// <param name="serviceLifetime">The service lifetime.</param>
        /// <returns>The IServiceCollection with the added extensions.</returns>
        public IServiceCollection AddWhatExecLib(ServiceLifetime serviceLifetime)
        {
            switch (serviceLifetime)
            {
                case ServiceLifetime.Scoped:
                    services.TryAddScoped<IFileSystem, FileSystem>();
                    services.TryAddScoped<IExecutableFileDetector, ExecutableFileDetector>();
                    services.TryAddScoped<
                        IExecutableFileInstancesLocator,
                        ExecutableFileInstancesLocator
                    >();
                    services.TryAddScoped<IExecutableInstancesLocator, ExecutableFileInstancesLocator>();
                    services.TryAddScoped<IExecutablesLocator, ExecutablesLocator>();
                    services.TryAddScoped<IPathEnvironmentVariableResolver, PathEnvironmentVariableResolver>();
                    break;
                case ServiceLifetime.Singleton:
                    services.TryAddSingleton<IFileSystem, FileSystem>();
                    services.TryAddSingleton<IExecutableFileDetector, ExecutableFileDetector>();
                    services.TryAddSingleton<
                        IExecutableFileInstancesLocator,
                        ExecutableFileInstancesLocator
                    >();
                    services.TryAddSingleton<IExecutableInstancesLocator, ExecutableFileInstancesLocator>();
                    services.TryAddSingleton<IExecutablesLocator, ExecutablesLocator>();
                    services.TryAddSingleton<IPathEnvironmentVariableResolver, PathEnvironmentVariableResolver>();
                    break;
                case ServiceLifetime.Transient:
                    services.TryAddTransient<IFileSystem, FileSystem>();
                    services.TryAddTransient<IExecutableFileDetector, ExecutableFileDetector>();
                    services.TryAddTransient<
                        IExecutableFileInstancesLocator,
                        ExecutableFileInstancesLocator
                    >();
                    services.TryAddTransient<IExecutableInstancesLocator, ExecutableFileInstancesLocator>();
                    services.TryAddTransient<IExecutablesLocator, ExecutablesLocator>();
                    services.TryAddTransient<IPathEnvironmentVariableResolver, PathEnvironmentVariableResolver>();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(serviceLifetime), serviceLifetime, null);
            }

            return services;
        }
    }
}