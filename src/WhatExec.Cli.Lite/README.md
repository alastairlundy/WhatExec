## WhatExecLite

![GitHub License](https://img.shields.io/github/license/alastairlundy/WhatExec?style=flat-square)
[![NuGet WhatExecLite.Cli](https://img.shields.io/nuget/v/WhatExec.Cli.Lite?style=flat-square)](https://www.nuget.org/packages/WhatExec.Cli.Lite)

### 🌟 Description

**WhatExecLite** is your trusty CLI tool for locating executables and command files on your system. Whether you're a seasoned developer or just starting out, WhatExecLite makes finding those elusive commands a breeze!

#### ⚠️ Important Note:
Support for **.NET 8** and **.NET 9** will be removed [when they become End-of-Life](https://dotnet.microsoft.com/en-us/download/dotnet) in November 2026.

### 📜 Table of Contents

- [Description](#description)
- [Installation](#installation)
- [Quick Start](#quick-start)
- [Examples](#examples)
- [Features](#features)
- [Contributing](#contributing)

---

## 📦 Installation

### For End Users (Recommended)

Install WhatExecLite as a .NET global tool:

```shell script
dotnet tool install -g WhatExec.Cli.Lite
```


Then, use it like this:

```shell script
whatexec-lite <cli command> <executable(s) to locate>
```


### For Contributors

If you're building locally for development, follow these steps:

```shell script
dotnet restore
dotnet build --configuration Release
```


Run the built binary from the output folder to test your changes.

---

## 🔎 Quick Start

Need to find `notepad.exe`? Let WhatExecLite do it for you!

```shell script
whatexec-lite find notepad.exe
```


Looking for multiple commands at once?

```shell script
whatexec-lite find git.exe node.exe code.exe
```


Limit the results to three per command:

```shell script
whatexec-lite find notepad.exe --limit 3
```


Enable interactive mode (prompts for input):

```shell script
whatexec-lite find --interactive
```


Show verbose output and timing information:

```shell script
whatexec-lite notepad.exe --verbose --report-time
```


---

## 📖 Examples

- **Locate a single executable:**

```shell script
whatexec-lite find notepad.exe
```

- **Locate multiple commands at once:**

```shell script
whatexec-lite find git.exe node.exe code.exe
```

---

## 📖 Additional Examples for Linux/macOS Users

- **Locate a command in macOS/Linux:**

```shell script
whatexeclite find python3
```


- **Search for multiple executables on Linux/macOS:**

```shell script
whatexeclite find git node npm
```


## 🎨 Features

- Discover executables and command files across PATH and common directories.
- Configure result limits (--limit / -l).
- Interactive mode for entering commands (-i / --interactive).
- Verbose output and timing (--verbose, --report-time).

### Contributing

We encourage you to read our [CONTRIBUTING.md](https://github.com/alastairlundy/whatexeclite/blob/main/CONTRIBUTING.md) file for details on contribution guidelines. Your contributions can help WhatExecLite become even better!

### License

WhatExecLite (CLI) is licensed under the [Mozilla Public License 2.0 (MPL 2.0)](https://github.com/alastairlundy/whatexeclite/blob/main/LICENSE.txt). See the LICENSE file for details.

---

### Note:
Remember to check your PATH and file permissions when using WhatExecLite on different platforms.