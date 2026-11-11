# Assembly loading and the manifest

## The module's dependencies

Measured 2026-11-10 on PowerShell 7.7.0, which runs on .NET 11.0.0, with the module's `net11.0` Debug build. The Microsoft.Extensions assemblies are named here without that prefix.

- .NET 11's shared framework, `Microsoft.NETCore.App` 11.0.0, holds nine Microsoft.Extensions assemblies, all 11.0.0.0: `Caching.Abstractions`, `Configuration.Abstractions`, `DependencyInjection.Abstractions`, `Diagnostics.Abstractions`, `FileProviders.Abstractions`, `Hosting.Abstractions`, `Logging.Abstractions`, `Options`, and `Primitives`. .NET 10.0.15's holds none. PowerShell 7.7.0 carries its own copy of the runtime, so the nine sit in its folder, where `pwsh.deps.json` lists them under the runtime pack, beside `ObjectPool`, which PowerShell adds itself.
- A `net11.0` build leaves those nine out: the SDK prunes the packages whose assemblies the shared framework holds, and the build's deps.json doesn't list them. The Shell project's output holds the other seven that the module's packages bring: `Configuration`, `Configuration.Binder`, `DependencyInjection`, `Diagnostics`, `Http`, `Logging`, and `Options.ConfigurationExtensions`.
- Importing the build's DLL loads no Microsoft.Extensions assembly. A cmdlet that fails for want of a connection loads `DependencyInjection.Abstractions` from PowerShell's folder. After `Connect-SonarrInstance` and `Get-SonarrTag` against the sandbox, `DependencyInjection`, `Diagnostics`, `Http`, and `Logging` were loaded from the build's folder, and `DependencyInjection.Abstractions`, `Diagnostics.Abstractions`, `Logging.Abstractions`, and `Options` from PowerShell's, all 11.0.0.0.
- `MG.Sonarr.Next.Shell.dll` references `System.Runtime` 11.0.0.0 and `System.Management.Automation` 7.7.0.500.

Measured 2026-10-25 on PowerShell 7.6.8 with the module's `net10.0` Debug build.

- PowerShell 7.6.8 ships one Microsoft.Extensions assembly in its own folder, `Microsoft.Extensions.ObjectPool` 10.0.0.0, and none of the others that the module uses.
- After a cmdlet ran, even one that failed for want of a connection, `Microsoft.Extensions.DependencyInjection` and `Microsoft.Extensions.DependencyInjection.Abstractions` 10.0.0.0 were loaded from the build's folder, beside the module's own assemblies, and no other Microsoft.Extensions assembly was.
- Importing the build's DLL, before any cmdlet runs, loads `Microsoft.Extensions.DependencyInjection.Abstractions` from the build's folder and no other Microsoft.Extensions assembly. When the folder lacks it, the import fails with "Could not load file or assembly 'Microsoft.Extensions.DependencyInjection.Abstractions, Version=10.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60'. The system cannot find the file specified.", although the package is in the NuGet cache and the build's deps.json lists it (measured 2026-10-25 on PowerShell 7.6.9).
- `MG.Sonarr.Next.Shell.dll` references `System.Runtime` 10.0.0.0 and `System.Management.Automation` 7.6+, and PowerShell 7.6+ loads and runs the build, including one compiled against a 7.6 preview of the package.

## Type names and lazy loading

PowerShell resolves a type name only against assemblies that are already loaded, and the runtime loads an assembly the first time code needs it. Importing a module's DLL loads only the referenced assemblies that the import itself needs, which for PoshSonarr's dependencies was `Microsoft.Extensions.DependencyInjection.Abstractions` alone on PowerShell 7.6.8 and none of them on PowerShell 7.7.0, so until a cmdlet runs, a type from one of the others isn't found: `'<type name>' -as [type]` is `$null` and a type literal fails with "Unable to find type" (seen 2026-10-07 on PowerShell 7).

## Loading a build's DLLs in a script

To inspect a build, such as counting the public types it exports with `Assembly.GetExportedTypes()`, load each DLL with `[Reflection.Assembly]::LoadFrom` in a new process, so the build output stays unlocked.

- Load two builds of the same assembly, with the same name and version, in separate processes. A second `LoadFrom` in the same process throws "Assembly with same name is already loaded". In a `foreach` loop that error doesn't stop the script, the variable keeps the first assembly, and the second path prints the first build's results under its own name (found 2026-10-07 on PowerShell 7). DLLs whose assembly names differ can share a process.

## The PowerShell 7 version

Measured 2026-10-10 on PowerShell 7.6.6 with a binary module's Release build, unless a fact gives another date.

- PowerShell 7.6.6 runs `System.Management.Automation` 7.6.0.500, and so does PowerShell 7.6.8 (measured 2026-10-25). An assembly compiled against the 7.6.6 package references 7.6.0.500 too, so the reference doesn't name the package's patch release.
- PowerShell 7.7.0 runs `System.Management.Automation` 7.7.0.500 on .NET 11.0.0, and an assembly compiled against the 7.7.0 package references 7.7.0.500 (measured 2026-11-10).
- PowerShell 7.6.0-rc.1 runs on .NET 10.0.2. There, importing the module's `net11.0` build by its DLL fails with a `FileNotFoundException`, "Could not load file or assembly 'System.Runtime, Version=11.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'. The system cannot find the file specified.", and importing it by its manifest, which requires 7.7, fails with `Modules_InsufficientPowerShellVersion`, whose message calls the version '7.6.0' (measured 2026-11-10).
- `$PSVersionTable.PSVersion` is a `SemanticVersion`, and `-ge [version]'7.6'` compares it with 7.6.0. It's `True` for 7.6.0, 7.6.6, 7.7.0-preview.1, and 8.0.0, and `False` for 6.2.7, 7.4.13, 7.5.4, 7.6.0-preview.5, and 7.6.0-rc.1.
- A child scope can't shadow `$PSVersionTable`: assigning to it fails with "Cannot overwrite variable PSVersionTable because it is read-only or constant." To try a version check with other versions, run its text with the variable renamed.

## A manifest whose root module is the DLL

Measured 2026-10-25 on PowerShell 7.6.8 with a throwaway binary module laid out like PoshSonarr's: the manifest names the DLL beside it as its `RootModule`, lists one of the DLL's two cmdlets in `CmdletsToExport` and that cmdlet's alias in `AliasesToExport`, and names a type file in `TypesToProcess`. Each cmdlet class declares an alias with `[Alias]`.

- `CmdletsToExport` decides which cmdlets are exported: the one it leaves out isn't.
- `AliasesToExport` doesn't limit the aliases that `[Alias]` declares: both were exported, so the alias of the cmdlet that wasn't exported was there too, and calling it failed with `CommandNotFoundException` ("The term '<cmdlet>' is not recognized as a name of a cmdlet, function, script file, or executable program. ...").
- `TypesToProcess` applies the type data on import. Importing the DLL by its path instead applies none of it and exports both cmdlets.
- `PowerShellVersion` is checked before the DLL loads. A manifest that requires 7.7 fails on PowerShell 7.6.8 with `Modules_InsufficientPowerShellVersion` ("The version of PowerShell on this computer is '7.6.8'. The module '<path>' requires a minimum PowerShell version of '7.7' to run. Verify that you have the minimum required version of PowerShell installed, and then try again."), and imports on PowerShell 7.7.0-preview.5, so the check ignores a prerelease label.

## What it means for PoshSonarr

- The manifest's `PowerShellVersion` is the first PowerShell release whose .NET can load what the module targets: 7.7 for `net11.0`. PowerShell 7.6 and earlier run on .NET 10 or earlier, which can't load an assembly that references `System.Runtime` 11.0.0.0, and the manifest's check turns that load failure into `Modules_InsufficientPowerShellVersion`, which names the version that the module needs. The check admits every 7.7 prerelease.
- Generate `CmdletsToExport` and `AliasesToExport` from the same cmdlet classes, and leave no cmdlet that declares an alias out of `CmdletsToExport`: its alias is exported anyway, and calling it fails.
- Ship every Microsoft.Extensions assembly that the module uses and .NET 11's shared framework lacks in the module's folder, since PowerShell doesn't look for one in the NuGet cache. The ones that the shared framework holds come with PowerShell 7.7's runtime, so a build that leaves them out, as the SDK's pruning does, is complete. Each loads the first time code needs it, not on import, so a test that only imports the module can't show that one is missing; run a cmdlet.
- A build against a 7.6 preview of the `System.Management.Automation` package references that preview's assembly version, which PowerShell 7.6+ loads.
- Run each build of the module in its own process. PowerShell 7 can't load two builds of the same assembly in one, and the first one loaded answers for both.
- A test or a script that names a type from one of the module's dependencies runs a cmdlet first, or loads the assembly, because the type isn't found by name right after the import.
