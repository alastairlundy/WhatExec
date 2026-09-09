---
title: Consolidate contracts into Lib
classification: Independent
blocked_by: []
parent: IMPLEMENTATION-whatexec-handoff-20260908.md
---

## Goal

Move all contract interfaces from the `WhatExec.Lib.Abstractions` project into `src/WhatExec.Lib` so a single project holds contracts beside implementations, and retire the Abstractions package as a separate dependency target.

## What to build

Move the six contract interfaces currently under `src/WhatExec.Lib.Abstractions/` - `IExecutablesLocator`, `IExecutableFileInstancesLocator` (Locators), `IPathEnvironmentVariableResolver`, `IExecutableFileResolver` (Resolvers), `IExecutableFileDetector`, `IPathEnvironmentVariableDetector` (Detectors) - into matching folders under `src/WhatExec.Lib` with `WhatExec.Lib.*` namespaces. Update every consumer (implementations, the CLI projects, the dependency injection registration project, and the tests project) to use the new namespaces and drop the Abstractions project reference. Then either delete the `WhatExec.Lib.Abstractions` project or convert it into a re-export shim (type forwards) with a deprecation note, per the ledger decision that it is kept only as a re-export shim or removed. The compatibility Obsolete pass on the old members is out of scope here - it lands with ticket 004.

## Size

- **Files** - approximately 12 (6 interface files moved, 2 csproj edits, usings updated across roughly 4 consumer files)

## Recommended Workflow

### Step 1 - Move interface files into Lib

Where: `src/WhatExec.Lib/Locators/`, `src/WhatExec.Lib/Resolvers/`, `src/WhatExec.Lib/Detectors/`

- Move each interface file from `src/WhatExec.Lib.Abstractions/<area>/` into the matching folder under `src/WhatExec.Lib`
- Change each namespace from `WhatExec.Lib.Abstractions.*` to `WhatExec.Lib.*`

Verify: `src/WhatExec.Lib.Abstractions` contains no interface files

### Step 2 - Update consumers and project references

Where: `src/WhatExec.Lib/`, `src/WhatExec.Cli/`, `src/WhatExec.Cli.Lite/`, `src/WhatExec.Lib.Extensions.DependencyInjection/`, `src/WhatExecLib.Tests/`

- Replace `WhatExec.Lib.Abstractions` using directives with the new Lib namespaces
- Remove `ProjectReference` entries pointing at `WhatExec.Lib.Abstractions` from every csproj that no longer needs it

Verify: a solution-wide search finds no remaining `WhatExec.Lib.Abstractions` using directives outside the shim (if kept)

### Step 3 - Retire the Abstractions project

Where: `src/WhatExec.Lib.Abstractions/`, `src/WhatExec.sln`

- Either delete the project from the solution or convert it to a type-forward re-export shim with a deprecation note in its csproj

Verify: `dotnet build` on the solution succeeds with the chosen retirement approach

### Step 4 - Confirm single-project contract ownership

Where: N/A

- Confirm contracts and implementations now live in the same Lib project so one edit updates contract and core together

Verify: solution builds and existing tests pass unchanged

## Context pointers

##### Files

- `src/WhatExec.Lib.Abstractions/` - source of the six interfaces being moved
- `src/WhatExec.Lib/Resolvers/PathEnvironmentVariableResolver.cs` - consumes `IPathEnvironmentVariableResolver` and detector interfaces; usings need updating
- `src/WhatExec.Lib/Locators/ExecutablesLocator.cs` - consumes `IExecutablesLocator`; usings need updating
- `src/WhatExecLib.Tests/` - test project reference updates

##### ADRs

None - the repo has no `docs/adr/` directory.

##### Domain terms

- Executable discovery - the locator contracts being moved define this seam
- Executable detection - the detector contracts being moved define this seam
- PATH resolution - the resolver contract being moved defines this seam

##### Ledger records

- `DECISIONS-whatexec-discovery-path.md#D012` - contracts move into Lib with Abstractions retired as a separate dependency target
- `DECISIONS-whatexec-discovery-path.md#D013` - single Lib project holds contracts and traversal so one edit updates both

## Acceptance criteria

- [ ] All six contract interfaces live in `src/WhatExec.Lib` under `WhatExec.Lib.*` namespaces (`#D012`)
- [ ] No project other than an optional type-forward shim references `WhatExec.Lib.Abstractions` (`#D013`)
- [ ] The solution builds and the existing test suite passes with the contracts relocated (`#D013`)

## Dependencies

**Blocked by** - None - can start immediately
