# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## 1.2.0

### Added
- Added `IExecutableInstancesLocator` with streaming `DirectoryInfo`, `DriveInfo`, and `SearchOption` overloads on top of `System.IO.Abstractions` `IFileSystem`.
- Added streaming `EnumerateExecutableFilePathsAsync` and a `TryGet` collector on `PathEnvironmentVariableResolver`. Both support single-name overloads with first-match semantics.
- Added `ExecutablesLocatorTests`, `ExecutableInstancesLocatorTests`, and `PathEnvironmentVariableResolverUnitTests`. They cover directory, drive, and across-drives lookup, executable filtering, skipped inaccessible entries, `SearchOption` behavior, cancellation, and casing. Tests run against the testing-helper fake filesystem.
- Added `System.IO.Abstractions 22.2.0` and `System.IO.Abstractions.TestingHelpers 22.2.0`.
- Added decision record for the discovery path and implementation tickets 001 to 004 covering contract consolidation, split-pair locators, PATH redesign, and CLI migration.

### Changed
- Moved six contract interfaces from `WhatExec.Lib.Abstractions` into `WhatExec.Lib` under `WhatExec.Lib.*` namespaces. Updated consumers in Lib, Cli, Cli.Lite, Extensions.DependencyInjection, and Tests.
- Rewrote `PathEnvironmentVariableResolver` around one internal `FindFirstPathMatchAsync` PATH walk. Batch lookups take a snapshot of names so repeat passes stay consistent.
- Renamed locator `Within*` methods to `In*`, such as `InDirectory`, `InDrive`, and `AcrossDrives`. Applied the rename in interfaces, implementations, CLI commands, and tests.
- Wired `FindCommand` to `IPathEnvironmentVariableResolver` plus `IExecutableInstancesLocator`, checking PATH first. Wired `FindAllCommand` to `IExecutableInstancesLocator`. Registered the new locator in DI.
- Migrated resolver test classes from NUnit assertions to TUnit syntax.
- Documented the Winget ACL traversal limit in `README.md` under Known issues and in the VsCode resolver test.
- Updated NuGet package metadata to `1.1.0` in Cli, Cli.Lite, Lib, and Extensions.DependencyInjection.
- **Runtime dependencies.** Updated `DotExtensions` 10.3.2 to 10.5.2, `DotMake.CommandLine` 3.5.0 to 3.7.0, `Microsoft.Extensions.DependencyInjection`, `Abstractions`, `Microsoft.Bcl.AsyncInterfaces`, `System.Linq.AsyncEnumerable` 10.0.8 to 10.0.12, `Spectre.Console` 0.56.0 to 0.57.2, `Polyfill` 10.8.1 to 11.3.0.
- **CI dependencies.** Updated `actions/checkout` 6.0.3 to 7.0.1, `actions/setup-dotnet` 5.3.0 to 6.0.0, `github/codeql-action/upload-sarif` 4.36.2 to 4.37.9, `step-security/harden-runner` 2.19.4 to 2.21.1, `ossf/scorecard-action` 2.4.3 to 2.4.4, `Meziantou.Analyzer` 3.0.101 to 3.0.262.
- **Testing dependencies.** Updated `TUnit` 1.44.39 to 1.68.17.

### Deprecated
- Marked `IExecutableFileResolver`, `ExecutableFileResolver`, and `IExecutableFileInstancesLocator` members `Obsolete`. They warn on use and removal waits one version.
- Kept legacy `Get*` and `Task[]` locator members and the `ExecutableFileLocated` event as `[Obsolete]` for migration. They sit outside the new locator contract.

### Removed
- Removed the `WhatExec.Lib.Abstractions` project from the solution and deleted its directory. Dropped its `ProjectReference` and central `PackageVersion` entry.
- Removed `IPathEnvironmentVariableDetector`, `PathEnvironmentVariableDetector`, and the `GetPathContents` and `GetPathExtensions` hooks. The resolver reads PATH data directly from the environment.
- Removed the `ExecutableFileLocated` event from the new `IExecutableInstancesLocator` and `PathEnvironmentVariableResolver`.
- Deleted empty CLI event handlers.

### Fixed
- Fixed a parallel-run race in `PathEnvironmentVariableResolverUnitTests`. Cases mutated process-wide `PATH` and `PATHEXT` and the windows overlapped, so lookups saw another case's values. Marked the environment-touching resolver test classes `[NotInParallel]`.

[1.2.0]: https://github.com/alastairlundy/whatexec/compare/1.1.0...HEAD