# Project CLI Readme

## Overview

This document serves as the README for the Command Line Interface (CLI) programs of the WhatExec project. It provides comprehensive information on how to install, use, and contribute to the project's CLI tools.

## Table of Contents

1. [WhatExec](#whatexec)
   - Description
   - Installation Instructions
   - Usage Examples
   - Features and Capabilities
   - License Information
2. [WhatExecLite](#whatexec-lite)
   - Description
   - Installation Instructions
   - Usage Examples
   - Features and Capabilities
   - License Information

## Whatexec

### Description
The WhatExec project's fully featured CLI focused on locating executable files, wherever they may be, on a system.

### Installation Instructions

- Provide step-by-step instructions to install `whatexec`.

### Usage Examples

- Include one or more examples of how to use `whatexec`.

### Features and Capabilities

- List the main features and capabilities of `whatexec`.

### License Information
WhatExec CLI is licensed under **MPL 2.0** license. See the [LICENSE file](https://github.com/alastairlundy/whatexec/blob/main/LICENSE.txt) for details

## WhatExecLite

### Description
A lightweight CLI specifically focused on resolving file paths from the PATH environment variable.

### Features and Capabilities
The main features of WhatExecLite are:
* A) resolving file paths from the PATH Environment variable.
* B) doing so very quickly
* C) with a small binary size
  
WhatExecLite makes use of .NET Trimming and NativeAOT.

### Installation Instructions

- Provide step-by-step instructions to install `whatexec-lite`.

### Usage Examples
**NOTE** Regardless of the number of commands passed as arguments, each resolved command has its file path printed to a new line in Standard Output.

WhatExecLite's assembly/binary name is ``whatexec-lite``. It accepts commands as it's 1st positional argument. The order of other options/arguments should not matter. 

#### Single Command
To resolve the file path of the .NET SDK CLI enter:
```bash
whatexec-lite dotnet
```

The output for this on a Linux based system is typically:
```
/usr/bin/dotnet
```

#### Multiple Commands
To resolve multiple file paths at once, add the space separated list of commands.

For example, to look for dotnet, git, and wc you'd enter
```bash
whatexec-lite dotnet git wc
```

The output or this on a Linux based system is typically:
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
