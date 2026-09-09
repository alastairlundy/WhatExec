---
title: PATH resolution redesign
classification: Independent
blocked_by: [001-consolidate-contracts-into-lib]
parent: IMPLEMENTATION-whatexec-handoff-20260908.md
---

## Goal

Rewrite `PathEnvironmentVariableResolver` around a streaming Enumerate core plus a TryGet batch collector, reading PATH data directly from the environment, so streaming and batch callers each get one obvious entry point.

## What to build

Rework `src/WhatExec.Lib/Resolvers/PathEnvironmentVariableResolver.cs` to expose `EnumerateExecutableFilePathsAsync(IEnumerable<string> executableNames, CancellationToken)` returning `IAsyncEnumerable<KeyValuePair<string, FileInfo>>` and `TryGetExecutableFilePathsAsync(IEnumerable<string> executableNames, CancellationToken)` returning `Task<IReadOnlyDictionary<string, FileInfo>>`, plus single-name overloads over the batch core using the `executableName` parameter. The batch parameter is `IEnumerable<string>` named `executableNames` and the core materializes one snapshot of it for repeat passes and Count sizing. Delete `IPathEnvironmentVariableDetector` and the `GetPathContents`/`GetPathExtensions` hooks - the resolver reads PATH directories and extensions directly from the environment. Remaining protected-virtual hooks become internal, and a single PATH-walk core serves both methods. Fault rules - first-match PATH semantics (a name yields only its first PATH match, not every extension hit), skip unauthorized entries, consistent casing, never start hot tasks, and the directory walk searches the passed directory rather than its root. The `ExecutableFileLocated` event is not carried onto the new seam. Add unit tests in `src/WhatExecLib.Tests/` stubbing environment variables per case.

## Size

- **Files** - approximately 5 (resolver rewrite, detector deletion, interface update, 1-2 test files)
- **Large Edits required** - the rewrite plus tests are expected to exceed 500 lines changed combined

## Recommended Workflow

### Step 1 - Draw the new contract shape

Where: `src/WhatExec.Lib/Resolvers/` (or `src/WhatExec.Lib.Abstractions/Resolvers/` if ticket 001 has not merged)

- Redefine the resolver contract with the Enumerate core, TryGet collector, and single-name overloads using `executableNames`/`executableName` parameters

Verify: contract compiles with the blueprint signatures and parameter names

### Step 2 - Delete the detector indirection

Where: `src/WhatExec.Lib/Detectors/PathEnvironmentVariableDetector.cs`, resolver constructor

- Delete `IPathEnvironmentVariableDetector` and its implementation
- Read PATH directories and PATHEXT extensions directly from the environment inside the resolver

Verify: no references to the detector remain in the solution

### Step 3 - Implement the single PATH-walk core

Where: `src/WhatExec.Lib/Resolvers/PathEnvironmentVariableResolver.cs`

- Materialize one snapshot of `executableNames` for repeat passes and Count sizing
- Implement one internal PATH-walk core serving both Enumerate and TryGet, with internal (not protected-virtual) hooks
- Apply first-match PATH semantics, authorized-entry skipping, consistent casing, and no hot tasks; search the passed directory rather than its root

Verify: both public methods delegate to the one core with no duplicated walk logic

### Step 4 - Add environment-stubbed unit tests

Where: `src/WhatExecLib.Tests/`

- Stub PATH and PATHEXT environment variables per test case
- Cover streaming enumeration, batch TryGet (found, not-found, and partial results), single-name sugar, and first-match semantics

Verify: new tests pass; the two existing integration tests remain unchanged

## Context pointers

##### Files

- `src/WhatExec.Lib/Resolvers/PathEnvironmentVariableResolver.cs` - the file being rewritten; its duplicated `InternalResolveFilePaths`/`InternalTryResolveFilePathsAsync` bodies collapse into one core
- `src/WhatExec.Lib/Detectors/PathEnvironmentVariableDetector.cs` - deleted by this ticket
- `src/WhatExecLib.Tests/Resolvers/PathExecutableResolverTests.cs` - existing resolver tests to extend or replace

##### ADRs

None - the repo has no `docs/adr/` directory.

##### Domain terms

- PATH resolution - this ticket is the deepened PATH resolution seam
- Executable detection - the detector the walk core still consults for per-file verdicts

##### Ledger records

- `DECISIONS-whatexec-discovery-path.md#D007` - streaming Enumerate core plus TryGet batch collector; Resolve, Get, and TryResolve dissolve to call-site one-liners; hooks become internal
- `DECISIONS-whatexec-discovery-path.md#D010` - fault rules including first-match PATH semantics and the passed-directory walk fix
- `DECISIONS-whatexec-discovery-path.md#D015` - resolver reads PATH data directly from the environment with the detector deleted; tests stub environment variables
- `DECISIONS-whatexec-discovery-path.md#D018` - batch Enumerate plus TryGet with `executableNames` for multi and `executableName` for single parameters
- `DECISIONS-whatexec-discovery-path.md#D020` - batch takes `IEnumerable<string>` with one materialized snapshot for repeat passes and Count sizing
- `DECISIONS-whatexec-discovery-path.md#D008` - no events on the new seam

## Acceptance criteria

- [ ] The resolver exposes the streaming Enumerate core and TryGet collector with single-name sugar using the agreed parameter names (`#D007`, `#D018`)
- [ ] `IPathEnvironmentVariableDetector` and the `GetPathContents`/`GetPathExtensions` hooks are deleted and PATH data is read from the environment (`#D015`)
- [ ] A single PATH-walk core serves both methods with internal hooks (`#D007`)
- [ ] Batch methods take `IEnumerable<string>` and materialize one snapshot (`#D020`)
- [ ] First-match PATH semantics hold and the walk searches the passed directory rather than its root (`#D010`)
- [ ] No events on the new seam and unit tests stub environment variables per case (`#D008`, `#D015`)

## Dependencies

**Blocked by** - [001-consolidate-contracts-into-lib](001-consolidate-contracts-into-lib.md) - the resolver contract must ship from Lib following the contract move
