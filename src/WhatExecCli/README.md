## WhatExec

### Table of Contents
- [Description](#description)
- [Installation](#installation)
- [Quick start](#quick-start)
- [Examples](#examples)
- [Features](#features)
- [Contributing](#contributing)
- [Troubleshooting](#troubleshooting)
- [License](#license)


### Description
WhatExec is a small cross-platform CLI that locates executable files and commands on a system. It searches common locations and can perform deeper searches to return the first (or N) locations for each provided command or executable filename.

### Required runtime
- .NET SDK 8.0 or newer is required to build the project or install/run as a .NET global tool. End users running a published self-contained binary do not need the SDK but must use a compatible runtime for their platform.

### Installation
Choose the installation method that matches your needs:

- Install as a .NET global tool (recommended for end users):

  ``dotnet tool install -g WhatExec``

  After installation invoke as:

  ``whatexec <cli command> <executable(s) to locate>``

- Development 
For contributors building locally, use:

  ``dotnet restore``
  ``dotnet build --configuration Release``

  Then run the built binary from the output folder for testing.

### Quick start
Locate the system path for `notepad.exe` (installed as a dotnet tool):

  ``whatexec find notepad.exe``

Locate multiple commands when installed as a tool:

  ``whatexec find git.exe node.exe``

Limit results to three locations per command:

  ``whatexec find notepad.exe --limit 3``

Enable interactive mode (prompts for input):

  ``whatexec find --interactive``

Enable verbose output and timing:

  ``whatexe notepad.exe --verbose --report-time``

### Examples
- Locate a single executable:

  ``whatexec find notepad.exe``

- Locate multiple commands at once:

  ``whatexec find git.exe node.exe code.exe``

- Search an entire drive using the advanced search commands (see repository Commands folder for details)

### Features
- Locate executables and command files across PATH and common directories
- Configurable result limits (--limit / -l)
- Interactive mode for entering commands (-i / --interactive)
- Verbose output and timing (--verbose, --report-time)
- Graceful handling of access errors and partial results

### Troubleshooting
- WinGet / package manager disclaimer: Applications installed via WinGet or other system package managers may be installed into protected or virtualized directories which can be hidden from standard file-system searches. If an app installed via WinGet does not appear in WhatExec results, check whether the app's installation directory is protected or run with elevated permissions.
- If you receive permission errors when searching deep directories, run with elevated privileges or use interactive mode to skip problematic locations.
- If commands are not found, ensure they are on the PATH environment variable or provide the full filename (e.g. notepad.exe).

### Contributing
Contributions welcome. To develop locally:

  ``dotnet restore``
  ``dotnet build``

Run the built binary from the output folder to test changes. Open issues or PRs on the repository and include tests where applicable.

### License
WhatExec (CLI) is licensed under the Mozilla Public License 2.0 (MPL 2.0). See the [LICENSE file](https://github.com/alastairlundy/whatexec/blob/main/LICENSE.txt) for details.
