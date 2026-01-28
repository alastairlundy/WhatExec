# WhatExecLib

## Description

WhatExecLib is a C# library that finds executable files in PATH Environment Variables, Directories or Drives.

WhatExecLib implements ``WhatExecLib.Abstractions``'s interfaces.

### Resolvers

| Scenario                                                                 | Interface                            | Class                               |
|--------------------------------------------------------------------------|--------------------------------------|-------------------------------------|
| Finding all Executable files on a Drive or Directory                     | ``IExecutablesResolver``             | ``ExecutableResolver``              |
| Finding all Executable files with the same name                          | ``IExecutableFileInstancesResolver`` | ``ExecutableFileInstancesResolver`` |
| Resolve Executable file paths                                            | ``IExecutableFileResolver``          | ``ExecutableFileResolver``          |
| Resolve an Executable file path using just the PATH Environment variable | ``IPathEnvironmentVariableResolver`` | ``PathEnvironmentVariableResolver`` |

## Installation

To install WhatExecLib, run the following command

``dotnet add package AlastairLundy.WhatExecLib``
