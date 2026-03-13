# WhatExec.Lib

## Description

WhatExec.Lib is a C# library that finds executable files in PATH Environment Variables, Directories or Drives.

WhatExec.Lib implements ``WhatExec.Lib.Abstractions``'s interfaces.

### Locators

| Scenario                                                  | Interface                           | Class                              |
|-----------------------------------------------------------|-------------------------------------|------------------------------------|
| Finding all Executable files on a Drive or in a Directory | ``IExecutablesLocator``             | ``ExecutablesLocator``             |
| Finding all Executable files with the same name           | ``IExecutableFileInstancesLocator`` | ``ExecutableFileInstancesLocator`` |

### Resolvers

| Scenario                                                                 | Interface                            | Class                               |
|--------------------------------------------------------------------------|--------------------------------------|-------------------------------------|
| Resolve the file paths of specified Executable files                     | ``IExecutableFileResolver``          | ``ExecutableFileResolver``          |
| Resolve an Executable file path using just the PATH Environment variable | ``IPathEnvironmentVariableResolver`` | ``PathEnvironmentVariableResolver`` |

### Detectors
| Scenario                                                                                                        | Interface                            | Class                               |
|-----------------------------------------------------------------------------------------------------------------|--------------------------------------|-------------------------------------|
| Detects whether a file is executable or not.                                                                    | ``IExecutableFileDetector``          | ``ExecutableFileDetector``          |
| Detects the contents of the PATH Environment Variable, and the Path Executable Extensions Variable.<sup>1</sup> | ``IPathEnvironmentVariableDetector`` | ``PathEnvironmentVariableDetector`` |

<sup>1</sup> - Executable extensions returns empty array on non-Windows platforms.

## Installation

To install WhatExecLib, run the following command

``dotnet add package WhatExec.Lib``

## License
This library is licensed under the terms of the [Mozilla Public License 2.0](https://www.mozilla.org/en-US/MPL/2.0/).
