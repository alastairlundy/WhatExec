/*
    WhatExecLib
    Copyright (c) 2025-2026 Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WhatExec.Lib.Abstractions;
using WhatExec.Lib.Abstractions.Detectors;
using WhatExec.Lib.Detectors;

namespace WhatExec.Lib.Extensions.DependencyInjection;

/// <summary>
/// Provides extension methods for registering and adding WhatExecLib functionality to the dependency injection container.
/// </summary>
public static class RegisterAddWhatExecLib
{
    /// <summary>
    /// Adds the AddWhatExecLib extension methods to IServiceCollection.
    /// </summary>
    /// <param name="serviceLifetime">The service lifetime.</param>
    /// <param name="services"></param>
    /// <returns>The IServiceCollection with the added extensions.</returns>
    public static IServiceCollection AddWhatExecLib(this IServiceCollection services, ServiceLifetime serviceLifetime)
    {
        switch (serviceLifetime)
        {
            case ServiceLifetime.Scoped:
                services.AddScoped<IExecutableFileDetector, ExecutableFileDetector>();
                services.AddScoped<IPathEnvironmentVariableDetector, PathEnvironmentVariableDetector>();
                services.AddScoped<
                    IExecutableFileInstancesResolver,
                    ExecutableFileInstancesResolver
                >();
                services.AddScoped<IExecutableFileResolver, ExecutableFileResolver>();
                services.AddScoped<IExecutablesResolver, ExecutablesResolver>();
                services.TryAddScoped<IPathEnvironmentVariableResolver, PathEnvironmentVariableResolver>();
                break;
            case ServiceLifetime.Singleton:
                services.AddSingleton<IExecutableFileDetector, ExecutableFileDetector>();
                services.AddSingleton<IPathEnvironmentVariableDetector, PathEnvironmentVariableDetector>();
                services.AddSingleton<
                    IExecutableFileInstancesResolver,
                    ExecutableFileInstancesResolver
                >();
                services.AddSingleton<IExecutableFileResolver, ExecutableFileResolver>();
                services.AddSingleton<IExecutablesResolver, ExecutablesResolver>();
                services.TryAddSingleton<IPathEnvironmentVariableResolver, PathEnvironmentVariableResolver>();
                break;
            case ServiceLifetime.Transient:
                services.AddTransient<IExecutableFileDetector, ExecutableFileDetector>();
                services.AddTransient<IPathEnvironmentVariableDetector, PathEnvironmentVariableDetector>();
                services.AddTransient<
                    IExecutableFileInstancesResolver,
                    ExecutableFileInstancesResolver
                >();
                services.AddTransient<IExecutableFileResolver, ExecutableFileResolver>();
                services.AddTransient<IExecutablesResolver, ExecutablesResolver>();
                services.TryAddTransient<IPathEnvironmentVariableResolver, PathEnvironmentVariableResolver>();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(serviceLifetime), serviceLifetime, null);
        }

        return services;
    }
}
