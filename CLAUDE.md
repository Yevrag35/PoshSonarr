# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

PoshSonarr is a binary PowerShell module, written in C#, that manages a Sonarr PVR instance through Sonarr's v3 API. The module's assemblies target `net11.0`, so the module loads only in PowerShell 7.7 or later, the first release on .NET 11. `MG.Sonarr.Next.Shell.dll` is the module's root assembly, and a Release build writes the `PoshSonarr.psd1` manifest next to it.

`src/Legacy` is a historical folder that is not a part of any workflow or active development.

## Commands

Run these from the repository root. Building needs a .NET 11 SDK.

### Build

- Build from the Bash tool with exactly `dotnet build PoshSonarr.slnx -c Debug --nologo -v q --no-incremental 2>&1 | tail -30`, not from PowerShell, and never with an absolute path, which Bash's backslash escaping mangles. `--no-incremental` makes every project recompile and report its warnings, while an incremental build skips compiling up-to-date projects and reports nothing for them, so don't run a plain build first.
- A build of the whole solution with that command averages 12.919 seconds in its slowest case, with the build server cold, and 3.055 seconds with the build server warm from an earlier build. When the output reports `Build succeeded.`, a `Time Elapsed` that short on its last line still means that every project recompiled, so don't rerun the build or look for other proof that it ran.
- To package the module, run the same command with `-c Release`. A post-build step runs `.tasks/publish_module.ps1` with `pwsh.exe`, which writes the manifest into the output folder. The script imports the built DLL, so the `pwsh.exe` on the `PATH` must be PowerShell 7.7 or later.
- Every build, Debug or Release, copies the runtime assemblies of the module's NuGet dependencies into the Shell project's output folder, where PowerShell looks for them. The folder lacks PowerShell's own assemblies and the Microsoft.Extensions abstractions that .NET 11's shared framework holds, such as `Microsoft.Extensions.Options`, because PowerShell and its runtime supply them. The `poshsonarr-pwsh-internals` skill covers which assembly loads from where.
- Build the solution, never a single project file. The Release post-build step finds its script through `$(SolutionDir)`, which a project-file build leaves undefined.
- There is no lint step. No style analyzer package is referenced and `EnforceCodeStyleInBuild` is off, so `dotnet build` accepts code that breaks the `.editorconfig` style rules, even the ones set to `error`.
- The code-quality (`CA`) rules that `.editorconfig` sets to `warning` do run in the build, and each warning from one marks a bug to fix: an `await` without `ConfigureAwait(false)`, a `CancellationToken` that isn't passed on, a blocking call inside an async method, a call that depends on the current culture or compares strings linguistically, or an unused private field. The SDK doesn't run these rules on the `netstandard2.0` projects of the analyzers and the generators, so code there needs the same care without the warnings.
- `MG.Sonarr.Next`, `MG.Sonarr.Next.Shell.Bases`, and `MG.Sonarr.Next.Shell` set `GenerateDocumentationFile`, so the build checks their XML doc comments and reports a warning for each problem: CS1591 for a public member without a comment, CS1574 for a `cref` that doesn't resolve, CS1584 for one whose syntax is wrong, such as `ReadOnlySpan{char}`, and CS1572 and CS1573 for `<param>` tags that don't match the parameters. No other project generates a documentation file, so the other projects' comments go unchecked. All three XML files reach the Shell project's output folder, so the packaged module carries them.

### Debugging

- Never connect to a Sonarr instance other than the Docker sandbox. Don't run `Debug.ps1`, don't point `Connect-SonarrInstance` or `Test-SonarrHost` at any other instance, and don't send requests to the instance that `.env` names. When a change needs checking against any instance but the sandbox, ask the user to do it.
- The Docker sandbox is the disposable Sonarr container that the `poshsonarr-sandbox` Compose project in `sandbox/sonarr/` runs. Building its images, starting, stopping, resetting, and testing it, and sending it requests, `Connect-SonarrInstance` included, are allowed. Docker commands touch only that project's containers, volumes, and images.
- `src/MG.Sonarr.Next.Shell/README.md` documents `Debug.ps1`, the user's script for debugging the module by hand: running it, the Visual Studio launch profiles that run it, the `.env` file that holds its connection settings, and its parameters. Read it before changing `Debug.ps1`, `.env.example`, or `src/MG.Sonarr.Next.Shell/Properties/launchSettings.json`, and update it along with any change to them.

### Sandbox

- `sandbox/sonarr/README.md` documents the sandbox: what each of its scripts does and takes, its baseline, the mock metadata service and the made-up shows it serves, the smoke tests, connecting to it, changing its port or Sonarr version, running Compose by hand, troubleshooting, and what it can't test. Read it before changing anything under `sandbox/sonarr/` or relying on what the sandbox holds, and update it along with any change to the sandbox.
- `pwsh -NoProfile -File sandbox/sonarr/start.ps1` starts the sandbox and seeds it, creating only what it lacks, so running it again changes nothing. `pwsh -NoProfile -File sandbox/sonarr/stop.ps1` stops it and keeps its data, and `pwsh -NoProfile -File sandbox/sonarr/reset.ps1` removes its containers and volumes, and with them everything Sonarr stored, then does everything `start.ps1` does.
- `pwsh -NoProfile -File sandbox/sonarr/test.ps1` runs the smoke tests, which expect the sandbox as a reset leaves it, so run `reset.ps1` first when its state is unknown.
- `pwsh -NoProfile -File sandbox/sonarr/logs.ps1` shows Sonarr's log, or the mock metadata service's with `-Service skyhook`. Its `-Follow` streams until it's interrupted, so run it only in the background.
- Connect the module with `Connect-SonarrInstance -Url http://127.0.0.1:18989 -ApiKey 0123456789abcdef0123456789abcdef`, without `-NoApiInPath`.
- A `docker compose` command run by hand takes `--project-name poshsonarr-sandbox --file sandbox/sonarr/compose.yaml`, as the scripts' calls do.

### Pester tests

- `.tests/README.md` documents the Pester suite: what the runner does and takes, what `setup.ps1` gives each test file, and how to write a test against the sandbox, from finding its fixtures to cleaning up and testing a failure. Read it before writing or changing a test, the runner, or `setup.ps1`, and update it along with any change to how the suite works.
- `pwsh -NoProfile -File .tests/run.ps1` builds the solution in Debug and runs the suite against the sandbox. `-NoBuild` reuses the last build, `-Reset` resets the sandbox first, `-Path` runs only the files it names, and `-Tag` runs only the tests with those tags.
- Any session may run the suite, but only through the runner.
- Never write a test that connects to an instance other than the Docker sandbox, reads `.env`, or references `Debug.ps1`.

### C# tests

- `src/MG.Sonarr.Next.Tests/README.md` documents the C# suite: what belongs in it, running it, its options and exit codes, how the project is set up on Microsoft.Testing.Platform, the helpers in its `Infrastructure/` folder, and how to write a test. Read it before writing or changing a test or the project, and update it along with any change to how the suite works.
- `dotnet test --solution PoshSonarr.slnx`, run from the repo root, builds the solution and runs the suite, which needs no network and no sandbox, and `--no-build` runs the last build.
- Never pass `--no-incremental` to `dotnet test`. For a full rebuild, build with the command under Build, then run `dotnet test --solution PoshSonarr.slnx --no-build`. Before passing any other option, check that `dotnet test --help`, run from the repo root, lists it.
- The source generators get no tests and no test project.

### Byte checks

- `pwsh -NoProfile -File tools/check-bytes.ps1` checks every file changed since `HEAD`, plus untracked files, for a BOM, CRLF, stray CRs, invalid UTF-8, and similar problems. Pass file paths, separated by spaces, to check only those; add `-Base realDev` to cover everything the branch changed, and `-Fix` to strip a BOM and turn CRLF into LF. The help at the top of the script lists every problem that it checks, and its exit codes.
- Not every file in the codebase adheres to the LF rule yet, so don't be alarmed when encountering one. Simply fix if the file is being edited currently or leave alone if not.

### Always-string check

- `pwsh -NoProfile -File tools/check-always-strings.ps1` reads Sonarr's source and checks that each property that can hold a string, in every body that a cmdlet sends back to Sonarr, is either on the always-string list in `src/MG.Sonarr.Next/Json/SonarrJsonOptions.cs` or in the script's table of held properties. The help at the top of the script says what it reports, what `-Verbose` adds, and its exit codes.
- It reads a clone of Sonarr's repository in `.temp/sonarr-src`, or the checkout that `-SonarrSource` names, checked out at the tag of the Sonarr version that `sandbox/sonarr/compose.yaml` pins. Git ignores `.temp/`, so each machine needs its own clone.
- Run it after changing the list, a model that a request sends back, or a cmdlet that sends one, and before moving the sandbox to a new Sonarr version. A cmdlet that starts sending a model back needs that model's resource class in the script's table of bodies.

### Disassembly

- `src/MG.Sonarr.Tools.Asm/README.md` documents the disassembly harness: running it, its build, where its output goes, reading the output, and its project references. The `poshsonarr-asm-dump` skill covers running it from a session, picking the methods to dump, and reading a dump. Read the skill before running the harness, and update both along with any change to the harness or `run-asmdump.ps1`.
- `pwsh -NoProfile -File src/MG.Sonarr.Tools.Asm/run-asmdump.ps1 <Class> [Method] -DontOpen` writes RyuJIT's FullOpts disassembly of the methods it names to `.temp/asm_<Class>[_<Method>].txt`. Always pass `-DontOpen`, or the script opens a viewer on the desktop.

## Projects

The module's projects under `src/` each reference the one before it:

1. `MG.Sonarr.Resources` (net11.0): message strings in `src/MG.Sonarr.Resources/Messages.resx` and exception messages in `src/MG.Sonarr.Resources/ExMessages.resx`, with public generated designer classes.
2. `MG.Sonarr.Next` (net11.0): the services library, with the HTTP client, JSON handling, metadata tags, the object models, and the module's base exceptions.
3. `MG.Sonarr.Next.Shell.Bases` (net11.0): the cmdlet base classes and the connection context.
4. `MG.Sonarr.Next.Shell` (net11.0): the cmdlets, the format and type files (`src/MG.Sonarr.Next.Shell/PSFormats/`, `src/MG.Sonarr.Next.Shell/PSTypes/`), and the debugging and packaging inputs.

`src/` also holds `MG.Sonarr.Next.Tests` (net11.0), the C# test suite, in the `Tests` solution folder. It references `MG.Sonarr.Next.Shell.Bases`, which brings `MG.Sonarr.Next`, and both name it in their `InternalsVisibleTo`. Its models are PSObjects and its cmdlet tests open runspaces outside PowerShell, so it references `System.Management.Automation` and `Microsoft.PowerShell.Commands.Utility` itself, which brings back the runtime assets that the module's projects leave out.

`src/` also holds `MG.Sonarr.Tools.Asm` (net11.0), the console app behind the disassembly script, which isn't part of the module. It references `MG.Sonarr.Next.Shell` to reach the types of every module project, and because it runs outside PowerShell, it references `System.Management.Automation` and `Microsoft.PowerShell.Commands.Utility` itself, which brings back the PowerShell assemblies that the module's projects leave out.

`src/` also holds `MG.Sonarr.Next.Generators` (netstandard2.0, so that both the SDK's compiler and Visual Studio's can load it), the module's source generators, in the `Analyzers` solution folder. `MG.Sonarr.Next` references it as an analyzer rather than as an assembly, so it reaches no output folder. Each generator injects its attribute, internal and in the `MG.Sonarr.Next.Generators` namespace, into the referencing project: `[GenerateEnumToString]` gives an enum a `<Enum>Extensions` class of formatting, validation, and parsing members, which copy through `SpanUnsafeExtensions` wherever the compilation can reach it and allows unsafe code, and `[RegisterService]` gives a class an `Add*` registration method on `IServiceCollection`, or reports `PSNR100` or `PSNR101` when the container couldn't build the registration, `PSNR102` when the generated class couldn't name a type, and `PSNR103` when `MethodName` isn't an identifier. Each generated class sits at namespace scope, so a nested type's class folds its containing types' names into its own, as in `OuterFooExtensions` for `Outer.Foo`. A new diagnostic needs a row in the project's `AnalyzerReleases.Unshipped.md`, or RS2000 warns. Referencing the generators from a project that `MG.Sonarr.Next`'s `InternalsVisibleTo` names makes CS0436 warn on the injected types, because that project also sees `MG.Sonarr.Next`'s internal copies of them.

`src/` also holds `MG.Sonarr.Next.Analyzers` (netstandard2.0, for the same reason as the generators), the module's diagnostic suppressors, in the `Analyzers` solution folder. An analyzer reference doesn't pass on to the projects that reference its holder, so `MG.Sonarr.Next`, `MG.Sonarr.Next.Shell.Bases`, and `MG.Sonarr.Next.Shell` each reference it as an analyzer, a new module project needs the same reference, and it reaches no output folder. `CollectionWithElementSuppressor` (`PSNRS001`) hides IDE0028 where its fix writes a collection expression with a `with(...)` element, which no `.editorconfig` option can single out, and keeps every other IDE0028. It reads details of Roslyn's code-style diagnostics that aren't a public contract, so an SDK update can break it, and the `with(...)` suggestions then come back. A suppression needs no row in an `AnalyzerReleases` file.

`src/Directory.Build.props` holds the shared assembly metadata, the `Version` (which is also the module version), the target framework, and the compiler settings: C# `preview` with the updated memory safety rules, nullable reference types, unsafe code allowed, and implicit usings plus a few extra global usings. A project replaces any of them by setting it in its own file, as the analyzers' and generators' projects do with `netstandard2.0`, and a project that multi-targets through `TargetFrameworks` must also set `TargetFramework` to empty, or the SDK builds it for `net11.0` alone. Keep C# at `preview`: the code uses features that the .NET 11 SDK ships only as previews, such as `safe` on an `extern` method, and fails to build under `latest`. Keep `updated-memory-safety-rules` in `Features` too: without it the code still builds, but `unsafe` on a member goes back to giving its body an unsafe context, and nothing makes a call to it, or to `Unsafe` or `MemoryMarshal`, sit in an `unsafe` block. Each `.csproj` can add more global usings through `<Using>` items. `src/Directory.Packages.props` manages package versions centrally; `System.Management.Automation` and `Microsoft.PowerShell.Commands.Utility` must stay on the same version. Its `Microsoft.CodeAnalysis.CSharp` version pins only the analyzers' and generators' own references, since transitive pinning is off, and it can't be newer than the compiler in the .NET SDK or Visual Studio, which won't load an analyzer or generator built against a newer Roslyn.

## Architecture

### Connection context

`Connect-SonarrInstance` builds a single static `ServiceProvider` (`SonarrContext` in `src/MG.Sonarr.Next.Shell.Bases/Context/CmdletContext.cs`), which replaces the one that an earlier connection built once it's built, and removes it again when its check of the URL and key fails. It holds the HTTP client, JSON options, metadata tags, and the Shell project's own services from `ModuleServiceConfigurer` (`src/MG.Sonarr.Next.Shell/Services/ServiceInitializer.cs`), and `Disconnect-SonarrInstance` removes it, or does nothing when there's no connection. `Test-SonarrHost` runs the same check on a provider of its own, which it disposes afterward, so it leaves the connection as it was. Every other cmdlet opens a DI scope on that provider for the length of its run, so until a connection exists, cmdlets fail with `ContextNotSetException`. Every runspace in the process shares the connection, and each scope holds a lease on the provider it came from, so a provider that a later connection or a disconnect removed is disposed once its last scope ends, and a cmdlet that runs while another runspace connects or disconnects finishes on the connection it started with. Builds other than Release turn on the container's `ValidateOnBuild` and `ValidateScopes` checks.

### Cmdlet base classes

The base classes in `src/MG.Sonarr.Next.Shell.Bases/Cmdlets/` form one chain: `SonarrCmdletBase` → `TimedCmdlet` → `SonarrApiCmdletBase` → `SonarrMetadataCmdlet`. Most cmdlets derive from one of the last two; `Connect-SonarrInstance`, `Disconnect-SonarrInstance`, and `Test-SonarrHost` use the separate `ConnectCmdlet`, `DisconnectCmdlet`, and `TestConnectionCmdlet` bases.

- `SonarrCmdletBase` seals PowerShell's processing methods. Cmdlets override `Begin`, `Process`, `End`, and `Stop`, which receive the scoped `IServiceProvider`, and set up state in `OnCreatingScope` rather than a constructor. Exceptions thrown from them become error records, written when they're thrown: one from `Begin` ends the cmdlet with its error, as `StopCmdlet(record)` does, so neither `Process` nor `End` runs, and one from `Process` writes its error and the cmdlet goes on to the next input, so `-ErrorAction` decides what happens to each, and under `Stop` the statement ends at the step that threw. The scope lasts until `EndProcessing` or `Dispose`, and `StopProcessing` only cancels `StoppingToken`, which the `Send*Request` helpers send with, and runs `Stop`. A cmdlet with `-Force` calls `ShouldProcessWithForce` instead of skipping `ShouldProcess`, so `-Force` skips only the confirmation prompt, and in What if mode the cmdlet shows its What if line and does nothing.
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
- CLAUDE.md is the hub of the repo's documentation, and every other file is a spoke. CLAUDE.md points at the READMEs and skills that hold each subject's details rather than repeating them, and keeps the commands that sessions run and the rules they follow. A spoke, whether a README, a skill, a plan, a script, or a code comment, states what it needs itself and never references CLAUDE.md or another document, such as a README or a skill, though it can name the scripts, config, and source files that it describes, and a skill can point at its own reference files.
- The updated memory safety rules are on, so a call to a member marked `unsafe`, such as most of `Unsafe` and `MemoryMarshal` and every member of `SpanUnsafeExtensions`, a call through a function pointer, and a `stackalloc` without an initializer in a `[SkipLocalsInit]` method compile only inside an `unsafe { }` block. Start each block with a `// SAFETY:` comment that says why each unchecked call in it is valid at that spot. Mark a member `unsafe` only when a caller can corrupt memory through it, and give it a `/// <safety>` section that states what its callers must guarantee; otherwise keep its signature safe and justify the block inside it. The `poshsonarr-performance` skill lists which members are marked and why.
- The `.editorconfig` C# style isn't enforced at build time, so match the surrounding code: file-scoped namespaces, `this.` on properties and methods but not fields, `_camelCase` private fields, keyword type names such as `string` and `int`, and regular constructors instead of primary ones.
- `[SharedCode]` (`src/MG.Sonarr.Next/Attributes/SharedCodeAttribute.cs`) marks each type that comes from the user's shared repository of code, which several of the user's solutions copy and adapt; a type without it isn't shared code. Keep a marked type and its members even when nothing calls them, and don't list them as code that needs tests. Pair the mark with `[ExcludeFromCodeCoverage]` wherever the type's target frameworks allow it. No framework allows it on an enum or a delegate, and none before .NET 11 on an interface, so the `IUnion` stand-in in `src/MG.Sonarr.Next/Unions/UnionCSharp15.cs`, which compiles only before .NET 11, goes without it. Whether a new type is shared code is the user's call, so ask before marking one.
- PRs target `realDev`; `master` only takes release PRs from `realDev`.
