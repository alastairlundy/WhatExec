---
title: Discovery split-pair locators
classification: Independent
blocked_by: [001-consolidate-contracts-into-lib]
parent: IMPLEMENTATION-whatexec-handoff-20260908.md
---

## Goal

Replace the current locator surfaces with the split-pair discovery seams from the blueprint - all-executables on one interface, named-instances on the other - backed by one shared traversal core that filters executability inside the implementation.

## What to build

Implement the blueprint contracts in `src/WhatExec.Lib/Locators/` - `IExecutablesLocator` with `EnumerateExecutablesInDirectoryAsync`, `EnumerateExecutablesInDriveAsync`, and `EnumerateExecutablesAcrossDrivesAsync`; and `IExecutableInstancesLocator` with the matching `EnumerateExecutableInstances*` overloads taking an executable name. Use per-scope overloads on `DirectoryInfo`/`DriveInfo` with `SearchOption` - no scope-union type. Across-drives methods are sugar over drive fan-out. One shared traversal core serves all six overloads; it runs `IExecutableFileDetector.IsFileExecutableAsync` on every file before yielding - no caller-side re-filter. Filesystem access goes through the System.IO.Abstractions seam (inject an `IFileSystem`). Fault rules - skip unauthorized entries (IgnoreInaccessible), consistent casing, fixed search patterns with no extension-first dead pattern, never start hot tasks. No events on the new seam. The module owns directory, drive, and all-drives traversal only - no PATH knowledge, no PATHEXT expansion, no PATH ordering. Add unit tests in `src/WhatExecLib.Tests/` targeting the new overloads through the System.IO.Abstractions test helper fake, with one detector mock covering every executability test. The two existing integration tests stay as-is.

## Size

- **Files** - approximately 7 (2 new interfaces, 2 locator implementations, 1 test support file, 2 test classes)
- **Large Edits required** - implementation plus tests are expected to exceed 500 lines combined

## Recommended Workflow

### Step 1 - Define the split-pair contracts

Where: `src/WhatExec.Lib/Locators/`

- Create `IExecutablesLocator` and `IExecutableInstancesLocator` matching the blueprint signatures exactly (directory, drive, and across-drives overloads per interface)
- Do not add events, progress callbacks, or a scope-union type

Verify: contracts compile and match the blueprint signatures

### Step 2 - Build the shared traversal core

Where: `src/WhatExec.Lib/Locators/`

- Implement one internal traversal core taking a starting directory, search option, and optional name filter, using `IFileSystem` for access
- Apply the fault rules - IgnoreInaccessible, consistent casing, fixed search patterns, no hot tasks
- Route every file through `IExecutableFileDetector` before yielding

Verify: the core compiles and handles both filtered and unfiltered traversal

### Step 3 - Wire the six public overloads

Where: `src/WhatExec.Lib/Locators/`

- Implement both locator classes delegating every overload to the shared core, with across-drives methods fanning out over `DriveInfo.GetDrives()`

Verify: all six overloads delegate to the single core with no duplicated traversal logic

### Step 4 - Add unit tests through the fake filesystem

Where: `src/WhatExecLib.Tests/`

- Add test classes using the System.IO.Abstractions test helper fake, one shared detector mock for executability cases
- Cover directory, drive, and across-drives scopes for both interfaces, executability filtering, and unauthorized-entry skipping

Verify: new tests pass and the existing integration tests remain untouched

## Context pointers

##### Files

- `src/WhatExec.Lib/Locators/ExecutablesLocator.cs` - existing implementation to replace; its inline `UnauthorizedAccessException` fallback and event raise are removed here
- `src/WhatExec.Lib/Locators/ExecutableFileInstancesLocator.cs` - existing instances locator to replace
- `src/WhatExec.Lib/Detectors/ExecutableFileDetector.cs` - the sole executability authority the core calls
- `src/WhatExecLib.Tests/` - where the new unit tests land

##### ADRs

None - the repo has no `docs/adr/` directory.

##### Domain terms

- Executable discovery - this ticket is the deepened discovery module
- Executable detection - the verdict authority used per file before yielding

##### Ledger records

- `DECISIONS-whatexec-discovery-path.md#D004` - keep finding executables and instances while reducing caller friction
- `DECISIONS-whatexec-discovery-path.md#D002` - discovery owns directory, drive, and all-drives traversal only - no PATH-first policy
- `DECISIONS-whatexec-discovery-path.md#D003` - executability is checked per file through detection before yielding - no caller-side re-filter
- `DECISIONS-whatexec-discovery-path.md#D008` - no events on the new seam
- `DECISIONS-whatexec-discovery-path.md#D009` - System.IO.Abstractions seam with its test helper fake; existing integration tests unchanged
- `DECISIONS-whatexec-discovery-path.md#D010` - fault rules - skip unauthorized, consistent casing, fixed patterns, never start hot tasks
- `DECISIONS-whatexec-discovery-path.md#D011` - traversal only in Lib; PATH-first composition stays in callers above both seams
- `DECISIONS-whatexec-discovery-path.md#D014` - all verdicts flow through `IExecutableFileDetector` - one mock covers every executability test
- `DECISIONS-whatexec-discovery-path.md#D016` - per-scope overloads on `DirectoryInfo`/`DriveInfo` - no scope-union type; one shared traversal core
- `DECISIONS-whatexec-discovery-path.md#D017` - streaming overloads plus AcrossDrives sugar with the executability filter inside the implementation
- `DECISIONS-whatexec-discovery-path.md#D019` - split pair - all-executables and named-instances on separate interfaces

## Acceptance criteria

- [ ] Both split-pair interfaces exist with exactly the blueprint overloads and no events (`#D019`, `#D017`, `#D008`)
- [ ] All six overloads are served by one shared traversal core with per-scope overloads on BCL types and no scope-union type (`#D016`)
- [ ] Every yielded file passed `IExecutableFileDetector` inside the implementation (`#D003`, `#D014`)
- [ ] Fault rules hold - unauthorized entries skipped, consistent casing, fixed search patterns, no hot tasks (`#D010`)
- [ ] Filesystem access goes through the System.IO.Abstractions seam and unit tests run against its test helper fake (`#D009`)
- [ ] The locator classes contain no PATH knowledge, PATHEXT expansion, or PATH ordering (`#D002`, `#D011`)

## Dependencies

**Blocked by** - [001-consolidate-contracts-into-lib](001-consolidate-contracts-into-lib.md) - the new contracts must ship from Lib following the contract move
