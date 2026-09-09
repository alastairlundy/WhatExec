# DECISIONS — whatexec discovery + PATH deepening (candidates 1 & 2)

Spec: `C:\Users\alast\AppData\Local\Temp\whatexec-handoff-20260908.md`
Glossary: `CONTEXT.md` (executable discovery / executable detection / PATH resolution)

### [D001] - session goal

- **Driver**: user wants the narrowed interfaces verified before any code changes land
- **Resolved Answer**: "Working toward concept then implementation"
- **Normalized Requirement**: The session shall resolve the discovery and PATH interfaces through concept alignment first, then implementation planning.
- **Constraints**: track: concept-then-implementation; D1-D6 from handoff confirmed open (re-grill all); ledger path confirmed by user

### [D002] - discovery scope

- **Driver**: user wants each CONTEXT.md term to have a single owner before signatures are drawn
- **Resolved Answer**: "Option B"
- **Normalized Requirement**: The discovery module shall own directory, drive, and all-drives traversal only and shall not implement PATH-first-then-scan policy.
- **Constraints**: PATH-first policy lives outside discovery in PATH resolution or its composer; discovery exposes no PATH entries, PATHEXT expansion, or PATH ordering

### [D003] - detection boundary

- **Driver**: user wants callers to receive only valid executables with no repeat logic
- **Resolved Answer**: "Option A"
- **Normalized Requirement**: The discovery implementation shall check executability per file through detection before yielding each result.
- **Constraints**: Cites D002; discovery depends on detection for every file visited; no caller-side re-filter path

### [D004] - session goal

- **Driver**: user wants usability improved and friction reduced without losing core find behavior
- **Resolved Answer**: "To clarify my overall session goal is to improve WhatExec usability, reduce friction points, whilst keeping the core functionality of finding executables and instances of executables."
- **Normalized Requirement**: The session shall produce interfaces that keep finding executables and instances while reducing caller friction.
- **Constraints**: Supersedes: D001; track stays concept-then-implementation; usability gains must not remove core find behavior

### [D005] - compatibility

- **Driver**: user wants existing consumers to migrate without breakage on update
- **Resolved Answer**: "Option A"
- **Normalized Requirement**: New narrow interfaces shall ship alongside old ones marked Obsolete with removal deferred one version.
- **Constraints**: Cites D004; old locator and resolver members carry Obsolete warnings, not errors; no silent shims without migration pressure

### [D006] - discovery shape

- **Driver**: user wants finding executables and instances to stay distinct caller intents
- **Resolved Answer**: "Option A"
- **Normalized Requirement**: Discovery shall expose EnumerateExecutablesAsync and EnumerateInstancesAsync over one scope union covering directory, drive, and all-drives.
- **Constraints**: Cites D002; Cites D003; Cites D004; one shared traversal core serves both enumerators; no per-scope overload explosion

### [D007] - PATH surface

- **Driver**: user wants streaming and batch callers to each get one obvious entry point
- **Resolved Answer**: "Option A"
- **Normalized Requirement**: PATH resolution shall expose a streaming Enumerate core plus a TryGet batch collector with Resolve, Get, and TryResolve dissolved to call-site one-liners.
- **Constraints**: Cites D002; Cites D004; protected-virtual hooks become internal; single PATH-walk core serves both methods

### [D008] - events

- **Driver**: user wants enumeration as the single way to observe results
- **Resolved Answer**: "Option A"
- **Normalized Requirement**: New discovery and PATH interfaces shall expose no events while old event members go Obsolete and empty Cli handlers are deleted.
- **Constraints**: Cites D005; push-style consumers rewrite to enumeration; no progress callback parameter added

### [D009] - test seam

- **Driver**: user prefers an established fake over owning a custom one
- **Resolved Answer**: "Option B"
- **Normalized Requirement**: The discovery filesystem seam shall use System.IO.Abstractions with its test helper backing the in-memory fake.
- **Constraints**: Cites D004; Cites D006; consumers inherit the Abstractions dependency; existing integration tests stay unchanged

### [D010] - fault behavior

- **Driver**: user wants preserved find behavior to be predictable instead of quirk-dependent
- **Resolved Answer**: "Option A"
- **Normalized Requirement**: New cores shall skip unauthorized entries, use consistent casing, fix search patterns, yield first PATH match, and never start hot tasks.
- **Constraints**: Cites D006; Cites D007; legacy edge-case differences documented at call sites; Resolver Root-directory walk corrected to the passed directory

### [D011] - layer boundaries

- **Driver**: user wants the new seam narrow with one owner per term
- **Resolved Answer**: "Option A"
- **Normalized Requirement**: Discovery in Lib shall hold traversal only with PATH-first policy composed in callers above both seams.
- **Constraints**: Cites D002; Cites D006; no PATH knowledge inside discovery; coordination stays out of the traversal core

### [D012] - dependency direction

- **Driver**: user wants one package to carry the whole contract
- **Resolved Answer**: "Option C"
- **Normalized Requirement**: Contracts shall move into Lib beside implementations with Abstractions retired as a separate dependency target.
- **Constraints**: Supersedes: D005; Obsolete migration path now lives in Lib; Abstractions package kept only as a re-export shim or removed

### [D013] - separation mechanism

- **Driver**: user wants one edit to update contract and core together
- **Resolved Answer**: "Option B"
- **Normalized Requirement**: The repo shall merge Abstractions into Lib so a single Lib project holds contracts and traversal.
- **Constraints**: Supersedes: D005; cross-package mirrors eliminated; published Abstractions package deprecated in favor of Lib

### [D014] - executability authority

- **Driver**: user wants every yielded file to pass the same verdict
- **Resolved Answer**: "Option A"
- **Normalized Requirement**: All executability verdicts shall flow through IExecutableFileDetector with inline PATHEXT and permission fallbacks removed from call sites.
- **Constraints**: Cites D003; Cites D010; detector internals own OS quirks; one mock covers every executability test

### [D015] - PATH data authority

- **Driver**: user wants one fewer interface between caller and PATH data
- **Resolved Answer**: "Option B"
- **Normalized Requirement**: The resolver shall read PATH directories and extensions directly from the environment with IPathEnvironmentVariableDetector deleted.
- **Constraints**: Cites D007; tests stub environment variables per case; GetPathContents and GetPathExtensions hooks deleted with the detector

### [D016] - discovery shape

- **Driver**: user finds the scope union unnatural and prefers overloads on known BCL types
- **Resolved Answer**: "Option A"
- **Normalized Requirement**: Discovery shall expose per-scope overloads on DirectoryInfo and DriveInfo for both enumerators with no scope-union type and all-drives handled by sugar or caller fan-out.
- **Constraints**: Supersedes: D006; Cites D002; Cites D003; Cites D004; Cites D011; DiscoveryScope family withdrawn before recording; one shared traversal core serves all overloads

### [D017] - discovery contract

- **Driver**: user wants drive-wide finds to stay one call with the filter inside
- **Resolved Answer**: "Option A"
- **Normalized Requirement**: The discovery contract shall expose four streaming overloads plus AcrossDrives sugar methods with the executability filter inside the implementation.
- **Constraints**: Cites D003; Cites D004; Cites D016; interface grouping decided in D019; no scope-union type

### [D018] - PATH contract

- **Driver**: user wants the common single lookup as a direct call with clear parameter names
- **Resolved Answer**: "Option B but the parameter gets renamed from "names" on multi name to "executableNames" and "executableName" on single string overload."
- **Normalized Requirement**: The PATH contract shall expose batch Enumerate plus TryGet with single-name overloads using executableNames for multi and executableName for single parameters.
- **Constraints**: Cites D004; Cites D007; Cites D010; Cites D015; batch parameter collection shape decided in D020

### [D019] - discovery grouping

- **Driver**: user wants commands dependent only on the shape they consume
- **Resolved Answer**: "Option A"
- **Normalized Requirement**: Discovery shall expose a split pair with all-executables overloads on one interface and named-instances overloads on the other, each with directory, drive, and across-drives methods.
- **Constraints**: Cites D004; Cites D016; Cites D017; single piled interface rejected; composite facade rejected

### [D020] - batch parameter type

- **Driver**: user wants the Microsoft-aligned least-specialized parameter shape
- **Resolved Answer**: "Option A"
- **Normalized Requirement**: Batch PATH methods shall take IEnumerable<string> executableNames with the core materializing one snapshot for repeat passes and Count sizing.
- **Constraints**: Cites D004; Cites D018; IReadOnlyList rejected per Microsoft Collection Parameters guidance; single-name overloads keep direct calls allocation-free

### [I001] - output target and PR count

- **Prompt**: Where should these tickets be published - local markdown files under the repo, or an issue tracker (GitHub Issues, GitLab Issues, Gitea Issues, Codeberg Issues, or a hosted Forgejo instance)? And should the tickets be grouped under a single pull request or split across several?
- **User Response**: "Local markdown files (Recommended)" and "1 PR (Recommended)".
- **Resolution**: Tickets will be written as local markdown files under the repo and grouped under a single pull request; blocked_by fields resolve to file basenames at publish time.
- **Notes**: User accepted both recommended defaults.

### [I002] - decomposition validation

- **Prompt**: Multi-part validation of the proposed four-ticket decomposition (contract consolidation, discovery locators, PATH resolver redesign, compatibility and CLI migration) - see closing questions in the session transcript.
- **User Response**: "Accept decomposition"
- **Resolution**: Four-ticket decomposition, dependency chain, and Independent classifications approved as proposed; proceeding to ticket generation for local markdown under a single pull request grouping.
- **Notes**: User gave a clear pass on all three closing questions; no tickets combined, split, or rescoped.

<!-- next-d: D021 -->
<!-- next-t: T001 -->
<!-- next-i: I003 -->
<!-- next-i: I001 -->
