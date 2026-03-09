# WhatExecLib.Abstractions

## Description

This is an abstractions-only library that provides interfaces for resolving Executable File locations.

### Locators

| Scenario                                                                 | Interface                            | 
|--------------------------------------------------------------------------|--------------------------------------|
| Finding all Executable files on a Drive or in a Directory                | ``IExecutablesLocator``              |
| Finding all Executable files with the same name                          | ``IExecutableFileInstancesLocator``  |

### Resolvers

| Scenario                                                                 | Interface                            | 
|--------------------------------------------------------------------------|--------------------------------------|
| Resolve the file paths of specified Executable files                     | ``IExecutableFileResolver``          |
| Resolve an Executable file path using just the PATH Environment variable | ``IPathEnvironmentVariableResolver`` |


## Installation

To install this package, run the following command:

``dotnet add package WhatExecLib.Abstractions``

## License
This library is licensed under the terms of the [Mozilla Public License 2.0](https://www.mozilla.org/en-US/MPL/2.0/).
