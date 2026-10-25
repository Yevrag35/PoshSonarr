# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

PoshSonarr is a binary PowerShell module, written in C#, that manages a Sonarr PVR instance through Sonarr's v3 API. The assemblies target `net10.0`, so the module loads only in PowerShell 7.6 or later, the first release on .NET 10. `MG.Sonarr.Next.Shell.dll` is the module's root assembly, and a Release build writes the `PoshSonarr.psd1` manifest next to it.

`src/Legacy` is a historical folder that is not a part of any workflow or active development.

## Commands

Run these from the repository root. Building needs a .NET 10 SDK.

### Build

- Build from the Bash tool with exactly `dotnet build PoshSonarr.slnx -c Debug --nologo -v q --no-incremental 2>&1 | tail -30`, not from PowerShell, and never with an absolute path, which Bash's backslash escaping mangles. `--no-incremental` makes every project recompile and report its warnings, while an incremental build skips compiling up-to-date projects and reports nothing for them, so don't run a plain build first.
- To package the module, run the same command with `-c Release`. A post-build step runs `.tasks/publish_module.ps1` with `pwsh.exe`, which copies the NuGet runtime dependencies listed in `src/MG.Sonarr.Next.Shell/build_dependencies.json` from the NuGet cache into the output folder (a class library's build output leaves them out, and PowerShell supplies `System.Management.Automation` itself), then writes the manifest.
- Build the solution, never a single project file. The Release post-build step finds its script through `$(SolutionDir)`, which a project-file build leaves undefined.
- There is no lint step. No analyzer package is referenced and `EnforceCodeStyleInBuild` is off, so `dotnet build` accepts code that breaks the `.editorconfig` style rules, even the ones set to `error`.

### Debugging

- After a Debug build, `pwsh -NoExit -File src/MG.Sonarr.Next.Shell/bin/Debug/net10.0/Debug.ps1` opens a session on it: the script copies the same dependencies into the output folder, imports the module, and loads the format and type files. The Visual Studio launch profiles run the same script.
- Connect with `Connect-SonarrInstance -Url <url> -ApiKey <key>` (alias `Connect-Sonarr`). Add `-NoApiInPath` (alias `-NoApiPrefix`) when a reverse proxy removes `/api` from the path.
- `src/MG.Sonarr.Next.Shell/debugging.json` names the development instance. Its `ApiKey` is a `{name}` placeholder for a PowerShell variable that holds the key, so a real key never goes in the file.

### Tests

- After a Debug build, `Invoke-Pester .tests/PoshSonarr.Tests.ps1` runs the tests; add `-FullNameFilter 'Get-SonarrSeries*'` to run one block.
- They're Pester 5.5+ integration tests that need a live, connected Sonarr instance, which may not be reachable from every machine, and they expect particular series to exist on it. `.tests/setup.ps1` imports the module first by dot-sourcing the `Debug.ps1` in the Shell project's build output. There are no unit tests.

### Byte checks

- `pwsh -NoProfile -File tools/check-bytes.ps1` checks every file changed since `HEAD`, plus untracked files, for a BOM, CRLF, stray CRs, invalid UTF-8, and similar problems. Pass file paths, separated by spaces, to check only those; add `-Base realDev` to cover everything the branch changed, and `-Fix` to strip a BOM and turn CRLF into LF. The script needs PowerShell 7.6 or later.
- Not every file in the codebase adheres to the LF rule yet, so don't be alarmed when encountering one. Simply fix if the file is being edited currently or leave alone if not.

## Projects

Each project under `src/` references the one before it:

1. `MG.Sonarr.Resources` (net10.0): message strings in `src/MG.Sonarr.Resources/Messages.resx` and exception messages in `src/MG.Sonarr.Resources/ExMessages.resx`, with public generated designer classes.
2. `MG.Sonarr.Next` (net10.0): the services library, with the HTTP client, JSON handling, metadata tags, the object models, and the module's base exceptions.
3. `MG.Sonarr.Next.Shell.Bases` (net10.0): the cmdlet base classes and the connection context.
4. `MG.Sonarr.Next.Shell` (net10.0): the cmdlets, the format and type files (`src/MG.Sonarr.Next.Shell/PSFormats/`, `src/MG.Sonarr.Next.Shell/PSTypes/`), and the debugging and packaging inputs.

`src/Directory.Build.props` holds the shared assembly metadata, the `Version` (which is also the module version), and the compiler settings: C# `preview`, unsafe code allowed, and implicit usings plus a few extra global usings. `MG.Sonarr.Resources` turns implicit usings off, so code there needs explicit `using` directives. Each `.csproj` can add more global usings through `<Using>` items. `src/Directory.Packages.props` manages package versions centrally; `System.Management.Automation` and `Microsoft.PowerShell.Commands.Utility` must stay on the same version.

## Architecture

### Connection context

`Connect-SonarrInstance` builds a single static `ServiceProvider` (`SonarrContext` in `src/MG.Sonarr.Next.Shell.Bases/Context/CmdletContext.cs`). It holds the HTTP client, JSON options, metadata tags, object pools, and the Shell project's own services from `ModuleServiceConfigurer` (`src/MG.Sonarr.Next.Shell/Services/ServiceInitializer.cs`), and `Disconnect-SonarrInstance` disposes it. Every other cmdlet opens a DI scope on that provider for the length of its run, so until a connection exists, cmdlets fail with `ContextNotSetException`. Builds other than Release turn on the container's `ValidateOnBuild` and `ValidateScopes` checks.

### Cmdlet base classes

The base classes in `src/MG.Sonarr.Next.Shell.Bases/Cmdlets/` form one chain: `SonarrCmdletBase` → `PoolableCmdlet` → `TimedCmdlet` → `SonarrApiCmdletBase` → `SonarrMetadataCmdlet`. Most cmdlets derive from one of the last two; `Connect-SonarrInstance` and `Disconnect-SonarrInstance` use the separate `ConnectCmdlet` and `DisconnectCmdlet` bases.

- `SonarrCmdletBase` seals PowerShell's processing methods. Cmdlets override `Begin`, `Process`, `End`, and `Stop`, which receive the scoped `IServiceProvider`, and set up state in `OnCreatingScope` rather than a constructor. Exceptions thrown from them become error records.
- `PoolableCmdlet` rents objects from pools registered in DI and returns them when the cmdlet is disposed.
- `SonarrApiCmdletBase` provides the synchronous `Send*Request` helpers. Each call queues the cmdlet on the singleton `ApiCmdletQueue`, which is how the HTTP handlers reach the running cmdlet to write its verbose and debug output, so API calls go through these helpers.
- `SonarrMetadataCmdlet` binds a cmdlet to one metadata tag, and so to one endpoint, and adds get-all, filter, and get-by-ID helpers.

### HTTP pipeline

`ISonarrClient` (`SonarrHttpClient`, registered by `SonarrClientDependencyInjection.AddSonarrClient`) is a typed `HttpClient` that sends the API key in the `X-Api-Key` header. A request passes through `PathHandler`, `VerboseHandler`, `DebugSerializeHandler`, `TestingHandler`, and `ErrorHandler`, in that order, before reaching `SonarrClientHandler`. The endpoint paths in `Constants` are relative; `PathHandler` adds the `/api/v3` prefix, or `/v3` under `-NoApiInPath`. Calls return a `SonarrClientResult` or `SonarrClientResult<T>`, which carries an error record instead of throwing.

### Objects and metadata tags

- Models derive from `SonarrObject`, which is a `PSObject`. Deserialization turns each JSON field into a PowerShell property at runtime, so fields that Sonarr adds appear without code changes, and typed C# properties exist only where the module's own code needs them.
- The first time a connection builds its JSON options, reflection creates a `SonarrObjectConverter<T>` for every public, non-abstract class marked `[SonarrObject]` in an assembly marked `[assembly: SonarrObjectConverterAssembly]`, which `src/MG.Sonarr.Next/MG.Sonarr.Next.csproj` sets. The attribute isn't inherited, so each model class needs its own.
- Objects get a `MetadataTag` property, such as `#series`, that names their endpoint and the cmdlets they can be piped to. A model picks its tag in `GetTag`, and `MetadataResolverDI.AddMetadata` registers each tag with its name from `Meta`, its endpoint path from `Constants`, and whether the endpoint takes an ID. A cmdlet's `[MetadataCanPipe(Tag = ...)]` attributes add its name to that tag's `CanPipeTo` list.
- Cmdlets take piped objects through the `I*Pipeable` interfaces that models implement, not through concrete model types.
- `SetPSTypeName` puts a model's full .NET type name first in its `TypeNames`, and the format and type files select objects by that name, so renaming or moving a model class means updating its `.ps1xml` files.

### Module manifest

`.tasks/publish_module.ps1` builds the manifest with `MG.Sonarr.Next.Shell.Build.Module`, which exports every public `[Cmdlet]` class in the Shell assembly along with its `[Alias]` names, plus every `*.Format.ps1xml` and `*.Type.ps1xml` file in the output folder. A new cmdlet needs only its attributes. A new format or type file needs that suffix and a `Content` item with `CopyToOutputDirectory` in `src/MG.Sonarr.Next.Shell/MG.Sonarr.Next.Shell.csproj`. A new runtime NuGet dependency also needs an entry in `src/MG.Sonarr.Next.Shell/build_dependencies.json`.

## Conventions

- Every text file is UTF-8 without a BOM, with LF line endings. The Edit tool keeps whatever BOM and line endings a file already has, so run the byte check on changed files before committing and fix what it reports.
- Indent with tabs in every file type, Markdown, JSON, XML, and PowerShell included. Project and XML config files set a tab width of 2, which is still one tab per level.
- The repo can sit at a different root on each machine, and a session can move to another machine at any point, so write every path in a checked-in file, commands included, relative to the repo root, and take the root from the current working directory. Root a location outside the repo in an environment variable, `$env:PROGRAMFILES` in prose and PowerShell and `$PROGRAMFILES` in Bash (Git Bash imports Windows variables in uppercase), and build paths in scripts from `$PSScriptRoot` or an MSBuild property such as `$(SolutionDir)`.
- The `.editorconfig` C# style isn't enforced at build time, so match the surrounding code: file-scoped namespaces, `this.` on properties and methods but not fields, `_camelCase` private fields, keyword type names such as `string` and `int`, and regular constructors instead of primary ones.
- PRs target `realDev`; `master` only takes release PRs from `realDev`.
