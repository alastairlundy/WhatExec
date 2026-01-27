/*
    WhatExecLib
    Copyright (c) 2025-2026 Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WhatExecLib.Abstractions;
using WhatExecLib.Abstractions.Detectors;
using WhatExecLib.Abstractions.Locators;
using WhatExecLib.Detectors;
using WhatExecLib.Locators;

namespace WhatExecLib.Extensions.DependencyInjection;

/// <summary>
/// Provides extension methods for registering and adding WhatExecLib functionality to the dependency injection container.
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
                    services.AddScoped<IExecutableFileDetector, ExecutableFileDetector>();
                    services.AddScoped<IPathEnvironmentVariableDetector, PathEnvironmentVariableDetector>();
                    services.AddScoped<
                        IExecutableFileInstancesLocator,
                        ExecutableFileInstancesLocator
                    >();
                    services.AddScoped<IExecutableFileLocator, ExecutableFileLocator>();
                    services.AddScoped<IExecutablesLocator, ExecutablesLocator>();
                    services.AddScoped<IMultiExecutableFileLocator, MultiExecutableFileLocator>();
                    services.TryAddScoped<IPathEnvironmentVariableResolver, PathEnvironmentVariableResolver>();
                    break;
                case ServiceLifetime.Singleton:
                    services.AddSingleton<IExecutableFileDetector, ExecutableFileDetector>();
                    services.AddSingleton<IPathEnvironmentVariableDetector, PathEnvironmentVariableDetector>();
                    services.AddSingleton<
                        IExecutableFileInstancesLocator,
                        ExecutableFileInstancesLocator
                    >();
                    services.AddSingleton<IExecutableFileLocator, ExecutableFileLocator>();
                    services.AddSingleton<IExecutablesLocator, ExecutablesLocator>();
                    services.AddSingleton<IMultiExecutableFileLocator, MultiExecutableFileLocator>();
                    services.TryAddSingleton<IPathEnvironmentVariableResolver, PathEnvironmentVariableResolver>();
                    break;
                case ServiceLifetime.Transient:
                    services.AddTransient<IExecutableFileDetector, ExecutableFileDetector>();
                    services.AddTransient<IPathEnvironmentVariableDetector, PathEnvironmentVariableDetector>();
                    services.AddTransient<
                        IExecutableFileInstancesLocator,
                        ExecutableFileInstancesLocator
                    >();
                    services.AddTransient<IExecutableFileLocator, ExecutableFileLocator>();
                    services.AddTransient<IExecutablesLocator, ExecutablesLocator>();
                    services.AddTransient<IMultiExecutableFileLocator, MultiExecutableFileLocator>();
                    services.TryAddTransient<IPathEnvironmentVariableResolver, PathEnvironmentVariableResolver>();
                    break;
            }

            return services;
        }
    }
}
