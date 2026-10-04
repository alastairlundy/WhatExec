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
- Deleted `ExecutableFileResolverTests.cs` (`Resolve_VsCode_ExecutableFile`, `Resolve_Random_Running_Process_ExecutableFiles`). The two host-environment-dependent tests required specific installed apps and running processes, so the suite is machine-independent without them.

### Fixed
- Fixed a parallel-run race in `PathEnvironmentVariableResolverUnitTests`. Cases mutated process-wide `PATH` and `PATHEXT` and the windows overlapped, so lookups saw another case's values. Marked the environment-touching resolver test classes `[NotInParallel]`.
- Fixed skipped-input, seam/hygiene, and fault-handling issues (`947a641`): allow relative PATH entries via `GetFullPath` instead of rejecting non-rooted paths; use OS-dependent casing for name filters, dictionary keys, and `MatchCasing`; normalize drive names (`C:` matches `C:\`) and exact-match the `--interactive` flag; Lite CLI returns 1 on partial misses and reports missing names; hardened `ExecutableFileDetector` `FileStream` sharing, single MZ check, per-OS try/catch, and `GetAwaiter().GetResult()`; skip `FileNotFound`/`DirectoryNotFound`/`IOException` in traversal core and the obsolete resolver; narrowed `GetDrives()` catch-alls with a symlink cycle guard and broader inaccessible-directory handling; fixed dead `AggregateException` handlers (`SkipWhile` to `Where`) and duplicate-key `Add` throws; routed traversal filename/existence checks through the `IFileSystem` seam; raised `ExecutableFileInstanceLocated` events, removed `DEBUG` stdout, unified DI `TryAdd`, and applied `HOME` `TrimEnd` uniformly.
- Fixed traversal, resolver, PATH, exit-code, limit, and retry-recursion bugs (`2a32c89`, findings #2-#10): `ExecutablesLocator` honors the `IFileSystem` seam (`FileInfo` via `FileInfo.New`, existence via the seam) and the detector reads through `IFileSystem` via a new optional constructor overload; guarded each lazy `MoveNext` so mid-iteration faults end the directory sequence instead of aborting the whole traversal and never swallow `OperationCanceledException`; `ExecutableFileResolver` no longer mutates the name list mid-iteration (snapshot per directory, removal deferred) and searches the requested directory instead of the drive root; PATH `~` expansion replacement is offset-correct and replaces all occurrences; exit codes handle duplicate arguments and empty find-all results; `find --limit` is honored via PATH-first plus capped drive scan; retry-after-fault only retries with a strictly smaller input so identical input returns gracefully instead of recursing unboundedly; added missing `[EnumeratorCancellation]` attributes (`CS8425`). Includes the `IFileSystem` DI registration fix.
- Fixed the permission-probe crash and eager access-control reads in `ExecutableFileDetector` (`513a3a7`): route all three call sites through `TryGetExecutePermission`, which treats an unreadable ACL as cannot-confirm and yields false while still propagating `FileNotFoundException`/`DirectoryNotFoundException` and never swallowing `OperationCanceledException`, so `search` and `search drive` no longer crash partway through a drive walk on locked files (`ERROR_SHARING_VIOLATION`); the probe is guarded in the detector rather than by widening the locator catch, so unrelated `InvalidOperationException`s are not masked. Reordered the operands so the cheap executable-extension check short-circuits the ACL read, avoiding a full ACL read on every file encountered during a walk.
- Fixed the `search directory` command handler (`ff364ca`): renamed `SearchDirectoryCommand.Run(CliContext)` to `RunAsync(CliContext)`, matching the `FindCommand`, `SearchCommand`, `SearchDriveCommand`, and `FindAllCommand` handler convention, so `search directory <existing-dir> --limit 5` enumerates results with exit 0 instead of printing usage help.
- Moved the `nameFilter` comparison in `TraversalCoreAsync` above the filesystem probes (`18f3d6c`): the cheap name comparison now runs before `FileInfo` construction and the existence probe, so filtered scans skip filesystem work for rejected names. Unfiltered scans are behavior- and order-identical.

[1.2.0]: https://github.com/alastairlundy/whatexec/compare/1.1.0...HEAD