# WhatExec.Lib.Extensions.DependencyInjection Package Documentation

![GitHub License](https://img.shields.io/github/license/alastairlundy/whatexec?style=flat-square)

[![NuGet WhatExec.Extensions.DependencyInjection](https://img.shields.io/nuget/v/WhatExec.Lib.Extensions.DependencyInjection?style=flat-square&label=WhatExec.Lib.Extensions.DependencyInjection%20NuGet)](https://www.nuget.org/packages/WhatExec.Lib.Extensions.DependencyInjection)

**WhatExec.Lib.Extensions.DependencyInjection** is a powerful NuGet package that simplifies integrating [WhatExec.Lib](https://github.com/alastairlundy/WhatExec) with dependency injection in your .NET applications.

## Getting Started

### Prerequisites
- Familiarity with the .NET ecosystem and dependency injection concepts.
- A project targeting **.NET Standard 2.0** or a newer TFM.
- To build the project, you need the **.NET 10 SDK or newer**.

### Installation

You can install **WhatExec.Lib.Extensions.DependencyInjection** via NuGet using the following command:

```shell script
dotnet add package WhatExec.Lib.Extensions.DependencyInjection
```

Or search for `WhatExec.Lib.Extensions.DependencyInjection` in your IDE's NuGet package manager.

### Usage

1. **Registering Extensions**: To register and configure WhatExec.Lib within your application's dependency injection container, use the extension methods provided by this package.

```csharp
using WhatExec.Lib.Extensions.DependencyInjection;

public void ConfigureServices(IServiceCollection services)
{
    // Register all extensions for ServiceLifetime.Scoped
    services.AddWhatExecLib(ServiceLifetime.Scoped);

    // Alternatively, specify Singleton or Transient
    services.AddWhatExecLib(ServiceLifetime.Singleton);
}
```


2. **Using the Registered Services**: Once registered, you can inject and use WhatExec.Lib components throughout your application.

```csharp
public class MyService
{
    private readonly IExecutableFileDetector _executableFileDetector;

    public MyService(IExecutableFileDetector executableFileDetector)
    {
        _executableFileDetector = executableFileDetector;
    }

    // Use the detected files as needed.
}
```


## Features

- **Convenient Dependency Injection**: Easily register and manage WhatExec.Lib components in your application's dependency injection container.
- **Flexible Service Lifetimes**: Choose from `Scoped`, `Singleton`, or `Transient` lifetimes for your registered services.
- **Simplified Configuration**: Quickly integrate WhatExec.Lib without manually configuring each component.

## License

This package is licensed under the [Mozilla Public License 2.0 (MPL-2.0)](../../LICENSE.txt).

---

Need help getting started? Open a new Discussion in the GitHub Discussions section for [WhatExec](https://github.com/alastairlundy/WhatExec/discussions).

Happy coding with WhatExec.Lib in your .NET applications! 🚀