# WhatExec.Benchmarks

BenchmarkDotNet throughput evidence for the WhatExec scan pipeline (ledger
`DECISIONS-whatexec-delete-obsolete-resolver.md` T006):

- **Directory walk** - `IExecutablesLocator.EnumerateExecutablesInDirectoryAsync`
  over a fixed real directory tree, in both top-level and all-subdirectories cases.
- **Drive fan-out single pass** - `IExecutablesLocator.EnumerateExecutablesAcrossDrivesAsync`,
  the single-pass across-drives core, across the machine's ready drives (top-level only
  so a hand run stays short). Machine-dependent: the enumerated set is the host's
  ready drives, so results are only comparable before/after on the same machine,
  never across hosts.

Both are tagged with `[MemoryDiagnoser]`, so allocated bytes appear next to timings -
use that to spot allocation regressions (for example, eager intermediate lists
returning) alongside raw speed.

## Running locally

From the repository root:

```shell
dotnet run -c Release --project src/WhatExec.Benchmarks
```

Arguments pass through to the BenchmarkDotNet switcher, e.g. to run one scenario:

```shell
dotnet run -c Release --project src/WhatExec.Benchmarks -- --filter *DriveFanOut*
```

## Execution policy

Benchmarks are run **by hand, locally, only** (ledger T008). They are deliberately not
wired into any workflow or CI job: no workflow file references this project and there
is no automatic performance gate. Future changes ship without automatic performance
verification - run these benchmarks manually whenever throughput evidence is needed.
