---
title: Compatibility and CLI migration
classification: Independent
blocked_by: [002-discovery-split-pair-locators, 003-path-resolution-redesign]
parent: IMPLEMENTATION-whatexec-handoff-20260908.md
---

## Goal

Ship the new narrow seams alongside the old ones marked Obsolete (warnings, not errors), remove the empty CLI event handlers, and point each command at only the split-pair shape it consumes - completing the migration path with removal deferred one version.

## What to build

Mark the old locator and resolver interfaces and their members `[Obsolete]` with warnings rather than errors, keeping the removal deferred one version so existing consumers migrate without breakage. Old event members (`ExecutableFileLocated` on the locator and resolver) go Obsolete together with their interfaces. Delete the three empty CLI event handlers. Update the CLI commands so each depends only on the split-pair shape it consumes - all-executables commands bind `IExecutablesLocator`, named-instances commands bind `IExecutableInstancesLocator` - and keep PATH-first composition in the callers above both seams rather than inside the traversal core. Confirm the migration pressure is real - no silent shims.

## Size

- **Files** - approximately 8 (2 old interfaces, 2 old implementations, 3-4 CLI command files)

## Recommended Workflow

### Step 1 - Mark the old seam members Obsolete

Where: `src/WhatExec.Lib/Locators/`, `src/WhatExec.Lib/Resolvers/`

- Apply `[Obsolete]` to the old locator and resolver interfaces and members, including their `ExecutableFileLocated` event members
- Keep warnings non-breaking - no error-as-obsolete severity, no silent shims

Verify: a build produces Obsolete warnings but no errors for old-seam usage

### Step 2 - Delete the empty CLI event handlers

Where: `src/WhatExec.Cli/`

- Find and delete the three empty event handlers subscribing to the locator/resolver events

Verify: no empty event handler subscriptions remain in the CLI projects

### Step 3 - Rebind commands to the split-pair seams

Where: `src/WhatExec.Cli/Commands/`

- Rebind each command to only the split-pair interface it consumes
- Compose PATH-first-then-scan in the command caller above both seams - the traversal core gains no PATH knowledge

Verify: each command resolves exactly one of the two new locator interfaces (plus the resolver where PATH-first applies)

### Step 4 - Full solution verification

Where: N/A

- Build the solution and run the full test suite including the untouched integration tests
- Note legacy edge-case differences at the call sites that now compose PATH-first

Verify: solution builds clean of new warnings other than the intentional Obsolete ones, and all tests pass

## Context pointers

##### Files

- `src/WhatExec.Lib/Locators/ExecutablesLocator.cs` - old locator implementation receiving Obsolete markers
- `src/WhatExec.Lib/Resolvers/PathEnvironmentVariableResolver.cs` - old resolver members receiving Obsolete markers
- `src/WhatExec.Cli/Commands/FindCommand.cs`, `src/WhatExec.Cli/Commands/FindAllCommand.cs`, `src/WhatExec.Cli/Commands/Search/` - commands to rebind and their empty event handlers
- `src/WhatExec.Cli/Program.cs` - composition root where PATH-first ordering may live

##### ADRs

None - the repo has no `docs/adr/` directory.

##### Domain terms

- Executable discovery - the split-pair seams the commands rebind to
- PATH resolution - the resolver seam whose old members go Obsolete
- Executable detection - unchanged authority both old and new seams call

##### Ledger records

- `DECISIONS-whatexec-discovery-path.md#D004` - usability gains must not remove core find behavior
- `DECISIONS-whatexec-discovery-path.md#D005` - new narrow interfaces ship alongside old ones marked Obsolete with removal deferred one version - warnings, not errors
- `DECISIONS-whatexec-discovery-path.md#D008` - old event members go Obsolete with their interfaces and the three empty CLI event handlers are deleted
- `DECISIONS-whatexec-discovery-path.md#D011` - PATH-first policy composed in callers above both seams
- `DECISIONS-whatexec-discovery-path.md#D019` - commands depend only on the split-pair shape they consume

## Acceptance criteria

- [ ] Old locator and resolver interfaces and members carry `[Obsolete]` warnings, not errors, with removal deferred one version (`#D005`)
- [ ] Old event members are Obsolete alongside their interfaces and the three empty CLI event handlers are deleted (`#D008`)
- [ ] Each command depends only on the split-pair shape it consumes (`#D019`)
- [ ] PATH-first composition lives in callers above both seams - the traversal core has no PATH knowledge (`#D011`)
- [ ] The solution builds and the full test suite including the two integration tests passes (`#D004`)

## Dependencies

**Blocked by** - [002-discovery-split-pair-locators](002-discovery-split-pair-locators.md) - rebinding commands requires the new locators to exist; [003-path-resolution-redesign](003-path-resolution-redesign.md) - Obsolete marking of the resolver overlaps the file being rewritten
