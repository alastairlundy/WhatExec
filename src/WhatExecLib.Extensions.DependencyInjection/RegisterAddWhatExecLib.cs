/*
    WhatExecLib
    Copyright (c) 2025 Alastair Lundy

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
///
/// </summary>
public static class RegisterAddWhatExecLib
{
    /// <param name="services"></param>
    extension(IServiceCollection services)
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="serviceLifetime"></param>
        /// <returns></returns>
        public IServiceCollection AddWhatExecLib(ServiceLifetime serviceLifetime)
        {
            switch (serviceLifetime)
            {
                case ServiceLifetime.Scoped:
                    services.AddScoped<IExecutableFileDetector, ExecutableFileDetector>();
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
