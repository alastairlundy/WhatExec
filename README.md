# WhatExec

![GitHub License](https://img.shields.io/github/license/alastairlundy/whatexec?style=flat-square)
 [![NuGet WhatExec.Cli](https://img.shields.io/nuget/v/WhatExec.Cli?style=flat-square)](https://www.nuget.org/packages/WhatExec.Cli) [![NuGet WhatExec.Cli.Lite](https://img.shields.io/nuget/v/WhatExec.Cli.Lite?style=flat-square)](https://www.nuget.org/packages/WhatExec.Cli.Lite)

Search for executables across PATH, directories and drives — quickly from the command line or from your .NET code.

This repository contains two command-line tools and reusable libraries:

- WhatExec.Cli (full CLI / dotnet tool)
- WhatExec.Cli.Lite (PATH resolving only lightweight CLI / dotnet tool)
- WhatExec.Lib.* (C# libraries used by the CLIs and available as a NuGet package)

## Quick start — Command-line tools (recommended)

The CLIs are the primary entry point for most users. They are designed to be installed and run as dotnet tools.

Install (from NuGet):

- Global install (recommended):
  - WhatExec.Cli:
    ```bash
    dotnet tool install --global WhatExec.Cli
    ```
  - WhatExec.Cli.Lite:
    ```bash
    dotnet tool install --global WhatExec.Cli.Lite
    ```

- Per-user install:
  ```bash
  dotnet tool install --tool-path ~/.dotnet-tools WhatExec.Cli
  ```

Update:
```bash
dotnet tool update --global WhatExec.Cli
```

Run the tools:
- Show help and available commands:
  ```bash
  whatexec --help
  whatexec-lite --help
  ```
- Typical workflow:
  - Use the CLI to search for an executable name, list matching files in PATH, or scan directories/drives for executables.
  - See each CLI’s README for detailed subcommands and examples.

See the dedicated CLI READMEs for full usage and examples:
- [WhatExec CLI README](https://github.com/alastairlundy/WhatExec/blob/main/src/WhatExecCli/README.md)
- [WhatExec Lite CLI README](https://github.com/alastairlundy/WhatExec/blob/main/src/WhatExecLiteCli/README.md)


## Libraries — WhatExec.Lib.*

If you want to embed executable-search functionality in your .NET application, use the WhatExec.Lib libraries.

### WhatExec.Lib.Abstractions (Abstractions)
Install via NuGet:
```bash
dotnet add package WhatExec.Lib.Abstractions
```

- [WhatExec.Lib.Abstractions README](https://github.com/alastairlundy/whatexec/blob/main/src/WhatExecLib.Abstractions/README.md)

### WhatExec.Lib (Main implementation)
Install via NuGet:
```bash
dotnet add package WhatExec.Lib
```

Documentation and implementation details are available in the library README:
- [WhatExec.Lib README](https://github.com/alastairlundy/whatexec/blob/main/src/WhatExecLib/README.md)


### WhatExec.Lib.DependencyInjection
Install via NuGet:
```bash
dotnet add package WhatExec.Lib.DependencyInjection
```



## Features

- CLI-first experience for quick interactive discovery of executables
- Lightweight CLI for fast queries in constrained environments
- Reusable C# libraries for integrating executable discovery into apps and tools
- Search sources: PATH environment variable, specific directories, entire drives (configurable)

(See each CLI and the library README for exact feature lists and platform notes.)


## Examples

- Install the CLI and run help to see commands:
  ```bash
  dotnet tool install --global WhatExec.Cli
  whatexec --help
  ```
- Add the library to a project:
  ```bash
  dotnet add package WhatExec.Lib
  ```

For concrete command examples and flags, consult:
- [WhatExec CLI README](https://github.com/alastairlundy/WhatExec/blob/main/src/WhatExecCli/README.md)
- [WhatExec Lite CLI README](https://github.com/alastairlundy/WhatExec/blob/main/src/WhatExecLiteCli/README.md)
- [WhatExecLib README](https://github.com/alastairlundy/whatexec/blob/main/src/WhatExecLib/README.md)

## Contributing
Contributions are welcome! Please follow the contribution guidelines:
- [CONTRIBUTING.md](https://github.com/alastairlundy/whatexec/blob/main/CONTRIBUTING.md)

## License

This project is licensed under the MPL 2.0 License. See the LICENSE file for details:
- [LICENSE](https://github.com/alastairlundy/whatexec/blob/main/LICENSE.txt)
