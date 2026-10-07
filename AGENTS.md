# AGENTS.md

## Build & test

- Solution lives in `src/`, not repo root. Build with `dotnet build src/WhatExec.sln` (`CONTRIBUTING.md`'s bare `dotnet build` fails from root — verified `MSB1003`).
- Run tests with `dotnet run --project src/WhatExecLib.Tests/WhatExecLib.Tests.csproj` — NOT `dotnet test` (verified: `dotnet test` fails on .NET 10 SDK because the VSTest target is unsupported by Microsoft.Testing.Platform; the test project is an `Exe` that runs via `dotnet run`).
- Suite is 36 tests and all must pass.
- Requires .NET 10 SDK (`net10.0` is a target everywhere; tests target `net10.0` only).
- No lint, format, editorconfig, or pre-commit config. Build + test is the full local verification. `Meziantou.Analyzer` runs as a build analyzer, so treat its warnings as signal.

## Packages & versioning

- Central Package Management is on (`src/Directory.Packages.props`, `ManagePackageVersionsCentrally=true`). Never put `Version=` on a `PackageReference`; add/update the `PackageVersion` entry in `Directory.Packages.props` instead.
- `UsePublishedPackages` defaults to `false` (local `ProjectReference`s). Leave it alone for local dev; CI's `publish-nuget.yml` (manual dispatch, `.github/workflows/`) sets it `true` with `-p:WhatExecLibVersion=...` to build CLIs against published NuGet packages.
- Libs and CLIs set `GeneratePackageOnBuild=true` — a plain build also emits nupkgs.

## Layout

- `src/WhatExec.Lib/` — core logic. `Locators/` (PATH/directory/drive search) + `Resolvers/` (executable-name resolution) + `Detectors/`. Targets `net10.0` only (`ImplicitUsings` off, `Nullable` on). Marked `IsTrimmable`; keep new code trim/AOT-safe.
- `src/WhatExec.Lib.Extensions.DependencyInjection/` — DI registration only (`net10.0` only).
- `src/WhatExec.Cli/` (tool command `whatexec`, DotMake.CommandLine + Spectre.Console) and `src/WhatExec.Cli.Lite/` (tool command `whatexec-lite`, ConsoleAppFramework, PATH-only). Different CLI frameworks — check the per-project `README.md` before adding commands/flags. Both target `net10.0` only, `PublishAot`/`PublishTrimmed` are `false`.
- `src/WhatExecLib.Tests/` — TUnit tests (`OutputType=Exe`), `net10.0` only, references `WhatExec.Lib` directly. Mocks the filesystem via `System.IO.Abstractions.TestingHelpers` (see `Locators/LocatorTestHelpers.cs`); follow that pattern for new tests.
- `src/WhatExecLib/` contains only stale `bin/`/`obj/` — no source, ignore it.
- Root `README.md` still references a `WhatExecLib.Abstractions` package/folder that no longer exists; don't treat it as source of truth.

## Gotchas

- Windows Winget install dirs deny traversal, so lookups there silently miss (documented in root `README.md` Known Issues). Don't "fix" this in locators.
- Compare executable names with `OrdinalIgnoreCase` (established convention in the traversal core's name filter); don't switch to platform-dependent comparison.
- Keep PRs small and single-issue; AI-generated code in a PR must declare which portions are AI-written (`CONTRIBUTING.md`).

## Issues

- Issues live as GitHub issues via `gh`; PRs are not an issue surface. Details in `docs/agents/issue-tracker.md`.
- Triage labels (`docs/agents/triage-labels.md`): `needs-triage`, `needs-info`, `ready-for-agent`, `ready-for-human`, `wontfix`.

## Domain docs

- Single-context layout per `docs/agents/domain.md`: vocabulary lives in root `GLOSSARY.md`, decisions in `docs/adr/`.
- Neither exists yet in this repo — per `domain.md`, proceed silently without flagging or creating them; use glossary terms when naming concepts once they exist, and surface ADR contradictions explicitly.
