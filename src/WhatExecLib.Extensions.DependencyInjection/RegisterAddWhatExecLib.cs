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
    /// <summary>
    ///
    /// </summary>
    /// <param name="services"></param>
    /// <param name="serviceLifetime"></param>
    /// <returns></returns>
    public static IServiceCollection AddWhatExecLib(
        this IServiceCollection services,
        ServiceLifetime serviceLifetime
    )
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
                services.AddScoped<IMultiExecutableLocator, MultiExecutableLocator>();
                services.TryAddScoped<IPathExecutableResolver, PathExecutableResolver>();
                services.AddScoped<IWhatExecutableResolver, WhatExecutableResolver>();
                break;
            case ServiceLifetime.Singleton:
                services.AddSingleton<IExecutableFileDetector, ExecutableFileDetector>();
                services.AddSingleton<
                    IExecutableFileInstancesLocator,
                    ExecutableFileInstancesLocator
                >();
                services.AddSingleton<IExecutableFileLocator, ExecutableFileLocator>();
                services.AddSingleton<IMultiExecutableLocator, MultiExecutableLocator>();
                services.TryAddSingleton<IPathExecutableResolver, PathExecutableResolver>();
                services.AddSingleton<IWhatExecutableResolver, WhatExecutableResolver>();
                break;
            case ServiceLifetime.Transient:
                services.AddTransient<IExecutableFileDetector, ExecutableFileDetector>();
                services.AddTransient<
                    IExecutableFileInstancesLocator,
                    ExecutableFileInstancesLocator
                >();
                services.AddTransient<IExecutableFileLocator, ExecutableFileLocator>();
                services.AddTransient<IMultiExecutableLocator, MultiExecutableLocator>();
                services.TryAddTransient<IPathExecutableResolver, PathExecutableResolver>();
                services.AddTransient<IWhatExecutableResolver, WhatExecutableResolver>();
                break;
        }

        return services;
    }
}
