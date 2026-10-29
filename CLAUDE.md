# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

PoshSonarr is a binary PowerShell module, written in C#, that manages a Sonarr PVR instance through Sonarr's v3 API. The module's assemblies target `net10.0`, so the module loads only in PowerShell 7.6 or later, the first release on .NET 10. `MG.Sonarr.Next.Shell.dll` is the module's root assembly, and a Release build writes the `PoshSonarr.psd1` manifest next to it.

`src/Legacy` is a historical folder that is not a part of any workflow or active development.

## Commands

Run these from the repository root. Building needs a .NET 10 SDK.

### Build

- Build from the Bash tool with exactly `dotnet build PoshSonarr.slnx -c Debug --nologo -v q --no-incremental 2>&1 | tail -30`, not from PowerShell, and never with an absolute path, which Bash's backslash escaping mangles. `--no-incremental` makes every project recompile and report its warnings, while an incremental build skips compiling up-to-date projects and reports nothing for them, so don't run a plain build first.
- To package the module, run the same command with `-c Release`. A post-build step runs `.tasks/publish_module.ps1` with `pwsh.exe`, which writes the manifest into the output folder.
- Every build, Debug or Release, copies the runtime assemblies of the module's NuGet dependencies into the Shell project's output folder, where PowerShell looks for them, because that project sets `CopyLocalLockFileAssemblies`, which a class library leaves off by default. PowerShell supplies `System.Management.Automation` and `Microsoft.PowerShell.Commands.Utility` itself, so their references in `src/MG.Sonarr.Next/MG.Sonarr.Next.csproj` exclude their runtime and native assets, and NuGet carries that exclusion to the packages they depend on and to every project that references `MG.Sonarr.Next`.
- Build the solution, never a single project file. The Release post-build step finds its script through `$(SolutionDir)`, which a project-file build leaves undefined.
- There is no lint step. No style analyzer package is referenced and `EnforceCodeStyleInBuild` is off, so `dotnet build` accepts code that breaks the `.editorconfig` style rules, even the ones set to `error`.

### Debugging

- Never connect to a Sonarr instance other than the Docker testbed. Don't run `Debug.ps1` or the Pester tests, don't point `Connect-SonarrInstance` at any other instance, and don't send requests to the instance that `.env` names. When a change needs checking against any instance but the testbed, ask the user to do it.
- The Docker testbed is the disposable Sonarr container that the `poshsonarr-testbed` Compose project in `testbed/sonarr/` runs, published on `127.0.0.1` at port 18989 unless `SONARR_TESTBED_PORT` sets another, with the fixed API key `0123456789abcdef0123456789abcdef`. Building its image, starting, stopping, resetting, and testing it, and sending it requests, `Connect-SonarrInstance` included, are allowed. Docker commands touch only that project's containers, volumes, and image.
- `Debug.ps1`, in the Shell project, is for the user's interactive debugging only, and the tests never reference it. After a Debug build, `pwsh -NoExit -File src/MG.Sonarr.Next.Shell/bin/Debug/net10.0/Debug.ps1` opens a session on it: the script imports the module, loads the format and type files, and connects to the development instance. The Visual Studio launch profiles run the same script.
- `Connect-SonarrInstance -Url <url> -ApiKey <key>` (alias `Connect-Sonarr`) makes the connection, and `-NoApiInPath` (alias `-NoApiPrefix`) is for a reverse proxy that removes `/api` from the path.
- `.env` in the repo root holds the development instance's settings as `KEY=value` lines: `SONARR_URL`, `SONARR_API_KEY`, and `SONARR_NO_API_IN_PATH=true` when a reverse proxy removes `/api`. Git ignores the file, so the real API key goes in it; on a machine that lacks one, the user copies `.env.example` to `.env` and fills in the key. The script's `-SonarrUrl`, `-ApiKey`, and `-NoApiInPath` parameters take precedence over the file, and when it ends up without a URL or key, the script warns and skips connecting.

### Tests

- Testing isn't set up yet. The Pester files in `.tests/` are incomplete: don't run them, treat them as working, or fix them up so they run. Never write a test that connects to an instance other than the Docker testbed, or one that references `Debug.ps1`. `.tests/setup.ps1` throws before the suite imports the module, so `Invoke-Pester` stops there.

### Byte checks

- `pwsh -NoProfile -File tools/check-bytes.ps1` checks every file changed since `HEAD`, plus untracked files, for a BOM, CRLF, stray CRs, invalid UTF-8, and similar problems. Pass file paths, separated by spaces, to check only those; add `-Base realDev` to cover everything the branch changed, and `-Fix` to strip a BOM and turn CRLF into LF. The script needs PowerShell 7.6 or later.
- Not every file in the codebase adheres to the LF rule yet, so don't be alarmed when encountering one. Simply fix if the file is being edited currently or leave alone if not.

### Disassembly

- `pwsh -NoProfile -File src/MG.Sonarr.Tools.Asm/run-asmdump.ps1 <Class> [Method] -DontOpen` writes RyuJIT's FullOpts disassembly of the methods it names to `.temp/asm_<Class>[_<Method>].txt`. It builds the solution in Release first, which also packages the module, and without `-DontOpen` it opens a viewer on the desktop.

## Projects

The module's projects under `src/` each reference the one before it:

1. `MG.Sonarr.Resources` (net10.0): message strings in `src/MG.Sonarr.Resources/Messages.resx` and exception messages in `src/MG.Sonarr.Resources/ExMessages.resx`, with public generated designer classes.
2. `MG.Sonarr.Next` (net10.0): the services library, with the HTTP client, JSON handling, metadata tags, the object models, and the module's base exceptions.
3. `MG.Sonarr.Next.Shell.Bases` (net10.0): the cmdlet base classes and the connection context.
4. `MG.Sonarr.Next.Shell` (net10.0): the cmdlets, the format and type files (`src/MG.Sonarr.Next.Shell/PSFormats/`, `src/MG.Sonarr.Next.Shell/PSTypes/`), and the debugging and packaging inputs.

`src/` also holds `MG.Sonarr.Tools.Asm` (net10.0), the console app behind the disassembly script, which isn't part of the module. It references `MG.Sonarr.Next.Shell` to reach the types of every module project, and because it runs outside PowerShell, it references `System.Management.Automation` and `Microsoft.PowerShell.Commands.Utility` itself, which brings back the PowerShell assemblies that the module's projects leave out.

`src/` also holds `MG.Sonarr.Next.Generators` (netstandard2.0, so that both the SDK's compiler and Visual Studio's can load it), the module's source generators, in the `Analyzers` solution folder. `MG.Sonarr.Next` references it as an analyzer rather than as an assembly, so it reaches no output folder. Each generator injects its attribute, internal and in the `MG.Sonarr.Next.Generators` namespace, into the referencing project: `[GenerateEnumToString]` gives an enum a `<Enum>Extensions` class of formatting, validation, and parsing members, which copy through `SpanUnsafeExtensions` wherever the compilation can reach it, and `[RegisterService]` gives a class an `Add*` registration method on `IServiceCollection`, or reports `PSNR100` or `PSNR101` when the container couldn't build the registration. A new diagnostic needs a row in the project's `AnalyzerReleases.Unshipped.md`, or RS2000 warns. Referencing the generators from a project that `MG.Sonarr.Next`'s `InternalsVisibleTo` names makes CS0436 warn on the injected types, because that project also sees `MG.Sonarr.Next`'s internal copies of them.

`src/Directory.Build.props` holds the shared assembly metadata, the `Version` (which is also the module version), and the compiler settings: C# `latest`, unsafe code allowed, and implicit usings plus a few extra global usings. Each `.csproj` can add more global usings through `<Using>` items. `src/Directory.Packages.props` manages package versions centrally; `System.Management.Automation` and `Microsoft.PowerShell.Commands.Utility` must stay on the same version. Its `Microsoft.CodeAnalysis.CSharp` version pins only the generators' own reference, since transitive pinning is off, and it can't be newer than the compiler in the .NET SDK or Visual Studio, which won't load a generator built against a newer Roslyn.

## Architecture

### Connection context

`Connect-SonarrInstance` builds a single static `ServiceProvider` (`SonarrContext` in `src/MG.Sonarr.Next.Shell.Bases/Context/CmdletContext.cs`). It holds the HTTP client, JSON options, metadata tags, object pools, and the Shell project's own services from `ModuleServiceConfigurer` (`src/MG.Sonarr.Next.Shell/Services/ServiceInitializer.cs`), and `Disconnect-SonarrInstance` disposes it. Every other cmdlet opens a DI scope on that provider for the length of its run, so until a connection exists, cmdlets fail with `ContextNotSetException`. Builds other than Release turn on the container's `ValidateOnBuild` and `ValidateScopes` checks.

### Cmdlet base classes

The base classes in `src/MG.Sonarr.Next.Shell.Bases/Cmdlets/` form one chain: `SonarrCmdletBase` → `PoolableCmdlet` → `TimedCmdlet` → `SonarrApiCmdletBase` → `SonarrMetadataCmdlet`. Most cmdlets derive from one of the last two; `Connect-SonarrInstance` and `Disconnect-SonarrInstance` use the separate `ConnectCmdlet` and `DisconnectCmdlet` bases.

- `SonarrCmdletBase` seals PowerShell's processing methods. Cmdlets override `Begin`, `Process`, `End`, and `Stop`, which receive the scoped `IServiceProvider`, and set up state in `OnCreatingScope` rather than a constructor. Exceptions thrown from them become error records.
- `PoolableCmdlet` rents objects from pools registered in DI and returns them when the cmdlet is disposed.
- `SonarrApiCmdletBase` provides the synchronous `Send*Request` helpers, which API calls go through. Each call starts the timer that the cmdlet's verbose output reports. The HTTP handlers reach the running cmdlet, to write its verbose and debug output, through the request: the cmdlet's DI scope holds it in an `ApiCmdletAccessor`, and the clients that the scope resolves put it in the options of every request they send.
- `SonarrMetadataCmdlet` binds a cmdlet to one metadata tag, and so to one endpoint, and adds get-all, filter, and get-by-ID helpers.

### HTTP pipeline

`ISonarrClient` (`SonarrHttpClient`, registered by `SonarrClientDependencyInjection.AddSonarrClient`) is a typed `HttpClient` that sends the API key in the `X-Api-Key` header. A request passes through `PathHandler`, `VerboseHandler`, `DebugSerializeHandler`, `TestingHandler`, and `ErrorHandler`, in that order, before reaching `SonarrClientHandler`. The endpoint paths in `Constants` are relative; `PathHandler` puts the URL base from `-Url`, if any, and the `/api/v3` prefix, or `/v3` under `-NoApiInPath`, in front of them. Calls return a `SonarrClientResult` or `SonarrClientResult<T>`, which carries an error record instead of throwing.

### Objects and metadata tags

- Models derive from `SonarrObject`, which is a `PSObject`. Deserialization turns each JSON field into a PowerShell property at runtime, so fields that Sonarr adds appear without code changes, and typed C# properties exist only where the module's own code needs them.
- The first time a connection builds its JSON options, reflection creates a `SonarrObjectConverter<T>` for every public, non-abstract class marked `[SonarrObject]` in an assembly marked `[assembly: SonarrObjectConverterAssembly]`, which `src/MG.Sonarr.Next/MG.Sonarr.Next.csproj` sets. The attribute isn't inherited, so each model class needs its own.
- Objects get a `MetadataTag` property, such as `#series`, that names their endpoint and the cmdlets they can be piped to. A model picks its tag in `GetTag`, and `MetadataResolverDI.AddMetadata` registers each tag with its name from `Meta`, its endpoint path from `Constants`, and whether the endpoint takes an ID. A cmdlet's `[MetadataCanPipe(Tag = ...)]` attributes add its name to that tag's `CanPipeTo` list.
- Cmdlets take piped objects through the `I*Pipeable` interfaces that models implement, not through concrete model types.
- `SetPSTypeName` puts a model's full .NET type name first in its `TypeNames`, and the format and type files select objects by that name, so renaming or moving a model class means updating its `.ps1xml` files.

### Module manifest

`.tasks/publish_module.ps1` builds the manifest with `MG.Sonarr.Next.Shell.Build.Module`, which exports every public `[Cmdlet]` class in the Shell assembly along with its `[Alias]` names, plus every `*.Format.ps1xml` and `*.Type.ps1xml` file in the output folder. A new cmdlet needs only its attributes. A new format or type file needs that suffix and a `Content` item with `CopyToOutputDirectory` in `src/MG.Sonarr.Next.Shell/MG.Sonarr.Next.Shell.csproj`. A new NuGet dependency needs nothing more, since the build copies it into the output folder, unless PowerShell supplies it, in which case its `PackageReference` takes `ExcludeAssets="runtime;native"`, as the ones for `System.Management.Automation` and `Microsoft.PowerShell.Commands.Utility` do.

## Conventions

- Every text file is UTF-8 without a BOM, with LF line endings. The Edit tool keeps whatever BOM and line endings a file already has, so run the byte check on changed files before committing and fix what it reports.
- Indent with tabs in every file type, Markdown, JSON, XML, and PowerShell included. Project and XML config files set a tab width of 2, which is still one tab per level. YAML is the one exception: it forbids tabs in indentation, so YAML files indent with two spaces per level, as `.editorconfig` sets.
- The repo can sit at a different root on each machine, and a session can move to another machine at any point, so write every path in a checked-in file, commands included, relative to the repo root, and take the root from the current working directory. Root a location outside the repo in an environment variable: `$env:PROGRAMFILES` in prose and PowerShell, `$PROGRAMFILES` in Bash (Git Bash imports Windows variables in uppercase), and `%PROGRAMFILES%` in a Visual Studio launch profile, which Visual Studio expands. Build paths in scripts from `$PSScriptRoot` or an MSBuild property such as `$(SolutionDir)`.
- The `.editorconfig` C# style isn't enforced at build time, so match the surrounding code: file-scoped namespaces, `this.` on properties and methods but not fields, `_camelCase` private fields, keyword type names such as `string` and `int`, and regular constructors instead of primary ones.
- PRs target `realDev`; `master` only takes release PRs from `realDev`.
