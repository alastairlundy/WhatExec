# Implementation Blueprint — whatexec discovery + PATH deepening

## Scope Binding

- Linked Spec: `C:\Users\alast\AppData\Local\Temp\whatexec-handoff-20260908.md`
- Decision Ledger: `docs/decisions/DECISIONS-whatexec-discovery-path.md`
- This blueprint is a context pointer valid ONLY for the linked spec and must not be applied to other specifications without explicit authorization.

## Discovery contracts (new, in Lib)

Contracts live in Lib beside implementations; the Abstractions package is deprecated in favor of Lib [`DECISIONS-whatexec-discovery-path.md#D013`], kept only as a re-export shim or removed [`DECISIONS-whatexec-discovery-path.md#D012`].

```csharp
public interface IExecutablesLocator
{
    IAsyncEnumerable<FileInfo> EnumerateExecutablesInDirectoryAsync(DirectoryInfo directory, SearchOption search, CancellationToken ct);
    IAsyncEnumerable<FileInfo> EnumerateExecutablesInDriveAsync(DriveInfo drive, SearchOption search, CancellationToken ct);
    IAsyncEnumerable<FileInfo> EnumerateExecutablesAcrossDrivesAsync(SearchOption search, CancellationToken ct);
}
public interface IExecutableInstancesLocator
{
    IAsyncEnumerable<FileInfo> EnumerateExecutableInstancesInDirectoryAsync(DirectoryInfo directory, string executableName, SearchOption search, CancellationToken ct);
    IAsyncEnumerable<FileInfo> EnumerateExecutableInstancesInDriveAsync(DriveInfo drive, string executableName, SearchOption search, CancellationToken ct);
    IAsyncEnumerable<FileInfo> EnumerateExecutableInstancesAcrossDrivesAsync(string executableName, SearchOption search, CancellationToken ct);
}
```

Split pair keeps all-executables and named-instances callers on separate seams [`DECISIONS-whatexec-discovery-path.md#D019`]. Per-scope overloads on `DirectoryInfo`/`DriveInfo` replace any scope-union type; no `DiscoveryScope` type is introduced [`DECISIONS-whatexec-discovery-path.md#D016`]. Across-drives sugar stays on the seam so drive-wide finds remain one call [`DECISIONS-whatexec-discovery-path.md#D017`].

## Discovery implementation (`src/WhatExec.Lib/Locators/`)

One shared traversal core serves all six overloads [`DECISIONS-whatexec-discovery-path.md#D016`]. The module owns directory, drive, and all-drives traversal only; PATH-first policy lives in callers above both seams [`DECISIONS-whatexec-discovery-path.md#D002`] [`DECISIONS-whatexec-discovery-path.md#D011`]. The executability check runs inside the implementation per file before yielding [`DECISIONS-whatexec-discovery-path.md#D003`], and every verdict flows through `IExecutableFileDetector` with inline PATHEXT and permission fallbacks removed from call sites [`DECISIONS-whatexec-discovery-path.md#D014`]. Filesystem access goes through the System.IO.Abstractions seam [`DECISIONS-whatexec-discovery-path.md#D009`]. Unified fault rules apply: skip unauthorized entries, consistent casing, fixed search patterns (no dead extension-first pattern), first-match PATH semantics where composed, never start hot tasks, and the resolver directory walk searches the passed directory rather than its root [`DECISIONS-whatexec-discovery-path.md#D010`]. No events on the new seam [`DECISIONS-whatexec-discovery-path.md#D008`].

## PATH resolution (`src/WhatExec.Lib/Resolvers/PathEnvironmentVariableResolver.cs`)

```csharp
IAsyncEnumerable<KeyValuePair<string, FileInfo>> EnumerateExecutableFilePathsAsync(IEnumerable<string> executableNames, CancellationToken ct);
Task<IReadOnlyDictionary<string, FileInfo>> TryGetExecutableFilePathsAsync(IEnumerable<string> executableNames, CancellationToken ct);
// single-name overloads over the batch core using string executableName
```

Streaming `Enumerate` core plus `TryGet` batch collector; `Resolve`, `Get`, and `TryResolve` dissolve to call-site one-liners [`DECISIONS-whatexec-discovery-path.md#D007`]. Batch pair plus single-name sugar with `executableNames`/`executableName` parameters [`DECISIONS-whatexec-discovery-path.md#D018`]. Batch takes `IEnumerable<string>` and the core materializes one snapshot for repeat passes and count sizing, per Microsoft least-specialized-parameter guidance [`DECISIONS-whatexec-discovery-path.md#D020`]. The resolver reads PATH directories and extensions directly from the environment; `IPathEnvironmentVariableDetector` and the `GetPathContents`/`GetPathExtensions` hooks are deleted [`DECISIONS-whatexec-discovery-path.md#D015`]. Protected-virtual hooks become internal and a single PATH-walk core serves both methods [`DECISIONS-whatexec-discovery-path.md#D007`]. No events on the new seam [`DECISIONS-whatexec-discovery-path.md#D008`].

## Compatibility and Cli

New narrow interfaces ship alongside old ones marked `[Obsolete]` (warnings, not errors) with removal deferred one version [`DECISIONS-whatexec-discovery-path.md#D005`]; the migration path lives in Lib following the contract move [`DECISIONS-whatexec-discovery-path.md#D012`]. Old event members go `[Obsolete]` with their interfaces [`DECISIONS-whatexec-discovery-path.md#D008`]. The three empty Cli event handlers are deleted [`DECISIONS-whatexec-discovery-path.md#D008`]. Commands depend only on the split-pair shape they consume, and PATH-first composition stays in callers [`DECISIONS-whatexec-discovery-path.md#D011`] [`DECISIONS-whatexec-discovery-path.md#D019`].

## Tests (`src/WhatExecLib.Tests/`)

Fake filesystem via the System.IO.Abstractions test helper behind the new seam; consumers inherit the Abstractions dependency [`DECISIONS-whatexec-discovery-path.md#D009`]. Unit tests target the new overloads through the fake; the 2 existing integration tests stay as-is [`DECISIONS-whatexec-discovery-path.md#D009`]. One mock covers every executability test through the sole-authority detector [`DECISIONS-whatexec-discovery-path.md#D014`]. Tests stub environment variables per case for PATH coverage [`DECISIONS-whatexec-discovery-path.md#D015`].

## Ledger Reference

`DECISIONS-whatexec-discovery-path.md#D002`, `#D003`, `#D004`, `#D005`, `#D007`, `#D008`, `#D009`, `#D010`, `#D011`, `#D012`, `#D013`, `#D014`, `#D015`, `#D016`, `#D017`, `#D018`, `#D019`, `#D020`. Superseded, not binding: `#D001` (by `#D004`), `#D006` (by `#D016`).
