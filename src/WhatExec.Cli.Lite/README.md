## WhatExecLite

![GitHub License](https://img.shields.io/github/license/alastairlundy/whatexec?style=flat-square)

[![NuGet WhatExec.Cli.Lite](https://img.shields.io/nuget/v/WhatExec.Cli.Lite?style=flat-square&label=WhatExec.Cli.Lite%20NuGet)](https://www.nuget.org/packages/WhatExec.Cli.Lite)


### Description
A lightweight CLI specifically focused on resolving file paths from the PATH environment variable.

### Features and Capabilities
The main features of WhatExecLite are:
* A) resolving file paths from the PATH Environment variable.
* B) doing so very quickly
* C) with a small binary size

WhatExecLite makes use of .NET Trimming and, for the CLI not distributed as a dotnet tool, NativeAOT.

### Installation Instructions

Install as a dotnet-tool via: 
```bash
 dotnet install WhatExec.Cli.Lite
 ```

### Usage Examples
**NOTE** Regardless of the number of commands passed as arguments, each resolved command has its file path printed to a new line in Standard Output.

WhatExecLite's assembly/binary name is ``whatexec-lite``. It accepts commands as its first positional argument. The order of other options/arguments should not matter.

#### Single Command
To resolve the file path of the .NET SDK CLI, enter:
```bash
whatexec-lite dotnet
```

The output for this on a Linux-based system is typically:
```
/usr/bin/dotnet
```

#### Multiple Commands
To resolve multiple file paths at once, add the space-separated list of commands.

For example, to look for dotnet, git, and wc you'd enter
```bash
whatexec-lite dotnet git wc
```

The output or this on a Linux-based system is typically:
```
/usr/bin/dotnet
/usr/bin/git
/usr/bin/wc
```

#### Help
To access the WhatExecLite CLI help screen enter:
```bash
whatexec-lite --help
```

### License Information
WhatExecLite CLI is licensed under **MPL 2.0** license. See the [LICENSE file](https://github.com/alastairlundy/whatexec/blob/main/LICENSE.txt) for details