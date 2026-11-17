# MG.Sonarr.Tools.Asm

A developer tool, never shipped with the module. It force-compiles named PoshSonarr methods at RyuJIT FullOpts so `DOTNET_JitDisasm` can capture their disassembly, which is how a claim about the code the JIT generates gets verified rather than assumed.

`run-asmdump.ps1` beside it is the entry point. It owns the build, the JIT environment variables, the stdout redirect, the current/previous rotation, and the viewer handoff. Running the exe directly works, but means setting all of that up by hand.

## Usage

Run it from the repo root:

```powershell
.\src\MG.Sonarr.Tools.Asm\run-asmdump.ps1 <Class> [Method] [-switches]
```

| Example | What it does |
|---|---|
| `... Wildcard IsMatch` | One method, every overload of it |
| `... WildcardSet` | Every declared method and constructor of the class |
| `... SpanStringBuilder .ctor` | Constructors only |
| `... RentedBuffer AsSpan -TypeArgs char` | Closes a generic declaring type |
| `... PooledBuffer+Enumerator MoveNext -TypeArgs int` | A nested type inside a generic one |
| `... TimedCmdlet -Assembly Next.Shell.Bases` | Looks the type up in one assembly only |
| `... Wildcard IsMatch -Inlines` | Also records why each call was or was not inlined |
| `... Wildcard IsMatch -DontOpen` | Skips the viewer |

A property name resolves to its `get_`/`set_` accessors. Pass a method name that doesn't exist to make the harness list every method declared on the type, which is the quickest way to find a compiler-generated name.

`Get-Help .\src\MG.Sonarr.Tools.Asm\run-asmdump.ps1 -Full` prints the help for every parameter.

## Building

Each run builds the whole solution in Release first, not just this project, and that build also packages the module. The `MG.Sonarr.Next.Shell` project's Release post-build step finds its packaging script through `$(SolutionDir)`, which only a solution build defines, so a Release build of this project alone fails there. The build has to be Release: with Debug-built assemblies the JIT compiles their methods at MinOpts even with tiering off, and each listing's header ends in `(MinOpts)` instead of `(FullOpts)`.

The build prints nothing unless it fails, and then only its errors. `-NoBuild` skips it and dumps the last Release build.

## Where output goes

Dumps land in the repo's `.temp\` folder as `asm_<Class>[_<Method>].txt`, **not** next to this project. `.temp` is gitignored, so a dump is never staged for commit. Override with `-OutFile`; a relative path still resolves under `.temp`. A nested type's dump keeps its outer type's name, as in `asm_PooledBuffer_Enumerator_MoveNext.txt`.

Each successful run first moves the existing dump into `.temp\prev\` under the same file name, so consecutive runs of the same command are diffable. A failed or 0-byte run changes neither file, so the dump stays the last good one and `prev\` the one before it. A failed run's partial output, if it wrote any, stays in `<dump name>.new`. Rotation is independent of `-DontOpen`, which suppresses only the viewer.

On success the script opens WinMerge on previous-vs-current, or Notepad++ on the dump when there is no previous one. Both default to their usual locations under `$env:PROGRAMFILES`; set `POSHSONARR_ASM_DIFF` and `POSHSONARR_ASM_VIEWER` to point elsewhere on a given machine rather than editing the script.

`-Inlines` adds `<dump name>.inlines.txt` beside the dump: every inlining decision the JIT made while compiling the dumped methods, with the reason for each call it left as a call. Those calls also print to the console, and the file moves into `prev\` along with its dump. The viewer still opens only the dump. The decisions come from the runtime's JIT-tracing events, since the release JIT has no inline dump of its own.

## Reading the output

The harness tags each method it prepares:

| Tag | Meaning |
|---|---|
| `direct` | `RuntimeHelpers.PrepareMethod` compiled it without running it: every non-virtual method, every override, and every virtual method on a struct or ref struct |
| `invoked` | A newslot virtual method on a class (an interface implementation, or a `virtual` member that isn't an override), compiled by calling it on an uninitialized instance with default arguments, because `PrepareMethod` can silently skip one |
| `thunked` | The same, called through an emitted thunk because a span-like return or parameter type rules out reflection |
| `virtual?` | A newslot virtual the harness couldn't call, such as one declared on an abstract class. Its listing is in the dump only when `PrepareMethod` happened to compile it |

`invoked` and `thunked` methods run until they throw. Compilation finishes before the first instruction runs, so the exception can't affect the listing, but any side effect before the first null dereference does happen.

A standalone listing is how a method compiles on its own. When a small method normally inlines into its caller, the caller's listing is the code that actually runs, so dump the caller when that matters.

A 0-byte dump means the `DOTNET_JitDisasm` pattern matched nothing that compiled. `-Summary` adds one line per JIT-compiled method to the dump, which tells a pattern that's too narrow apart from a method that never compiled.

## Project references

The single `ProjectReference` to `MG.Sonarr.Next.Shell` transitively covers the module's projects, which is what gets every `MG.Sonarr` assembly copied next to the exe. The harness enumerates `MG.Sonarr*.dll` in its own output folder to build the type search, so a project the Shell project doesn't reference needs a `ProjectReference` of its own in `MG.Sonarr.Tools.Asm.csproj` before its types can be dumped.

The module's projects keep PowerShell's assemblies out of their build output, since PowerShell supplies them to the module. The harness runs outside PowerShell, so it references the `System.Management.Automation` and `Microsoft.PowerShell.Commands.Utility` packages itself, which copies them and everything they depend on next to the exe. Without them, a type built on one of PowerShell's, such as a cmdlet base class, can't load, and the type search doesn't find it.
