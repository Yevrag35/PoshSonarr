---
name: poshsonarr-asm-dump
description: Dump and read the RyuJIT FullOpts disassembly of PoshSonarr methods with `src/MG.Sonarr.Tools.Asm/run-asmdump.ps1`, and the JIT's inlining decisions with `-Inlines`. Covers running it from a session, why it builds the solution in Release, where dumps and their previous baselines land, selecting a method, its overloads, constructors, an accessor or indexer, an explicit interface implementation, a local function, a lambda's display class, a nested type, and generic types and methods (shared `__Canon` code, nullable and constrained type arguments), what the `direct`, `invoked`, `thunked`, and `virtual?` tags mean and which methods the harness runs to compile them, reading the inline trace and its reasons, and why a dump comes back empty. Use it to verify a claim about generated code instead of reasoning it out, such as whether a call inlines and why not, whether a bounds check or a span's reference hoist survives, or whether a refactor changed the emitted code, to compare assembly before and after an edit, to find out why a hot path runs slower than expected, and whenever a dump comes back empty or a method is tagged `virtual?`.
---

# Dumping RyuJIT disassembly for PoshSonarr methods

`src/MG.Sonarr.Tools.Asm` is a console project in the solution, in its `/Tools/` folder, that force-compiles named methods at RyuJIT FullOpts so that `DOTNET_JitDisasm` can print their disassembly. When a change or an answer depends on what the JIT generates, verify it with a dump instead of reasoning it out.

Run it through the script beside it, never the exe:

```powershell
.\src\MG.Sonarr.Tools.Asm\run-asmdump.ps1 <Class> [Method] [-switches]
```

- In a session, always pass `-DontOpen`. Without it, a successful run opens WinMerge or Notepad++ on the desktop. Read the file that the script's `output :` line names instead.
- From the Bash tool, run `pwsh -NoProfile -File src/MG.Sonarr.Tools.Asm/run-asmdump.ps1 Wildcard IsMatch -DontOpen`, with single quotes around any argument that holds `<`, `>`, `|`, `?`, or a backtick.
- The script derives the JIT pattern, the harness's arguments, and the dump's file name from the same `-Class` and `-Method`, sets `DOTNET_TieredCompilation=0` and `DOTNET_JitDisasm` for the run only, and restores the session's values afterwards. `Get-Help .\src\MG.Sonarr.Tools.Asm\run-asmdump.ps1 -Full` prints every parameter.
- When you measure something about the harness or the JIT that later work could depend on, add it here with the .NET version and the date, stated as a rule rather than as a story.

## Builds

- Each run first builds `PoshSonarr.slnx` in Release, which takes a few seconds and also packages the module into the Shell project's Release output. `-NoBuild` skips the build and dumps the last Release build, so pass it only when nothing under `src/` has changed since.
- The build targets the solution because the Shell project's Release post-build step finds `.tasks/publish_module.ps1` through `$(SolutionDir)`, which only a solution build defines. A Release build of the harness project alone fails there with MSB3073 (measured 2026-10-25 with .NET SDK 10.0.402).
- It has to be Release. With Debug-built assemblies the JIT compiles their methods at MinOpts even with tiering off, and each listing's first line ends in `(MinOpts)` instead of `(FullOpts)`: `Wildcard.IsMatch(string)` came to 127 bytes instead of 62 (measured 2026-10-25 on .NET 10.0.13).
- The build prints nothing unless it fails, and then only its errors.

## Output files

- Dumps land in the repo's `.temp/` folder, which is gitignored, as `.temp/asm_<Class>[_<Method>].txt`. A relative `-OutFile` also resolves under `.temp/`. A nested type's file keeps its outer type's name, as in `asm_PooledBuffer_Enumerator_MoveNext.txt`.
- A successful run first moves the existing dump into `.temp/prev/` under the same name, so the next run of the same command can be compared with it, and then opens WinMerge on the two, or Notepad++ on the dump when there's no previous one. `POSHSONARR_ASM_DIFF` and `POSHSONARR_ASM_VIEWER` point the script at the viewers on a machine where they're installed elsewhere.
- A failed or 0-byte run changes neither file, so after one, the dump file still holds the last good run, not this one. A failed run's partial output, if it wrote any, is in `<dump file>.new`.
- Disassembly goes to the dump. Every line the harness prints itself goes to the console.

## Selecting targets

| Invocation | Selects |
|---|---|
| `... Wildcard IsMatch` | Every overload of one method |
| `... WildcardSet` | Every declared method and constructor |
| `... SpanStringBuilder .ctor` | Constructors only |
| `... MetadataResolver Item` | An indexer, through the property fallback to `get_Item` and `set_Item` |
| `... Wildcard 'System.ISpanFormattable.TryFormat'` | An explicit interface implementation, by its full member name |
| `... RentedBuffer AsSpan -TypeArgs char` | A method of a closed generic type |
| `... PooledBuffer+Enumerator MoveNext -TypeArgs int` | A nested type inside a generic one |
| `... TimedCmdlet -Assembly Next.Shell.Bases` | A type looked up in one assembly only, named in full or without its `MG.Sonarr.` prefix |

- A property name selects its `get_` and `set_` accessors. For an indexer renamed with `[IndexerName("X")]`, pass `X`. A method's overloads are always selected together.
- To find a member's exact name, pass a method name that doesn't exist. The harness then lists every method declared on the type, compiler-generated ones included.
- The type search covers the `MG.Sonarr*` assemblies that the harness reaches through its reference to `MG.Sonarr.Next.Shell`, which references the module's other projects. A type in a project outside that closure needs a `ProjectReference` in `src/MG.Sonarr.Tools.Asm/MG.Sonarr.Tools.Asm.csproj` first.
- The harness references the `System.Management.Automation` and `Microsoft.PowerShell.Commands.Utility` packages itself, because the module's projects keep PowerShell's assemblies out of every project that references them. Keep those references: without them, a type built on one of PowerShell's, such as `SonarrCmdletBase`, can't load, and the search reports no type of that name (measured 2026-10-25 on .NET 10.0.13).
- The default pattern, `*<Class>*:*`, covers the whole class on purpose: it also catches state machines, local functions, and lambda display classes that a `*Class:Method` pattern would miss. It holds only what the harness compiles because, with tiering off, little else is JIT-compiled at all: a whole run compiled 39 methods, and everything else it ran was precompiled (measured 2026-10-25 on .NET 10.0.13). A `-Pattern` of your own needs a leading `*`, because the JIT matches the namespace-qualified class name.

### Generic types and methods

- `-TypeArgs` closes the declaring type, and `-MethodArgs` (alias `-MethodTypeArgs`) closes a generic method's own type parameters. Both take aliases such as `int` and `string`, BCL full names, and MG.Sonarr type names.
- A missing `-TypeArgs` ends the run with exit code 2 and the type's arity. A generic method without `-MethodArgs` is skipped on its own, as `skip [needs --margs]`, and a wrong count shows up as that method's `MakeGenericMethod` error.
- When two types share a simple name, the `-TypeArgs` count picks one: `RentedBuffer` with `-TypeArgs char` is `RentedBuffer<T>`, and without it the static `RentedBuffer` factory class.
- A reference-type argument compiles to shared code, listed with `System.__Canon`, as in ``RentedBuffer`1[System.__Canon]:AsSpan()``. That shared code is what runs for every reference type, so it's the right listing, not a degraded one.
- The C# nullable shorthand works: `-TypeArgs 'int?'` or `'WildcardMatchType?'` closes over `Nullable<T>`, and `'string?'` is `string`, as it is at runtime. The raw reflection form, ``'System.Nullable`1[System.Int32]'``, needs a BCL full name inside the brackets and can't name an MG.Sonarr type there, because assembly-qualifying one takes a comma, and a comma separates `-TypeArgs` values.
- A type argument that breaks a constraint reports `violates the constraint of type 'T'`, as `MetadataList` does with `int`, while one that doesn't resolve reports `type argument '...' not found`. The first means the name resolved and the type is wrong for that slot.

### Nested types

- Name a nested type as `Outer+Inner`. When the outer type is generic, the nested type shares its type parameters, so it needs `-TypeArgs` too, as in `PooledBuffer+Enumerator MoveNext -TypeArgs int`. Leaving `-TypeArgs` out ends the run with exit code 2.
- A nested type's simple name alone, such as `Enumerator`, matches every type of that name, and the harness lists them and stops.

### Local functions and lambdas

- The compiler emits a local function as a method of its declaring type, so a class-wide run prepares it with no extra arguments. To target one, pass its exact compiled name, quoted for PowerShell's angle brackets and pipe, as in `SpanStringBuilder '<Grow>g__throwTooBig|71_0'`.
- A local function declared inside a generic method has that method's type parameters, so it needs `-MethodArgs`, as `throwFormatEx` inside `SpanStringBuilder.AppendCore<T>` does.
- The numbers at the end of a compiled name follow the method's position in its type, and they change when members above it are added or moved. A saved command line goes stale that way, and so does the dump name it derives. Pass a stable `-OutFile` for any comparison across edits, and for runs of one generic type with different type arguments, which would otherwise share a dump name and rotate over each other's baseline.
- A lambda compiles into a method of a nested display class, `<>c` or `<>c__DisplayClass*`. Target the class through its outer type, as in `'Messenger+<>c'`. The bare `'<>c'` is ambiguous, and its pattern also matches the harness's own lambdas, which compile during every run.

## Reading the result

The harness tags each method it prepares:

| Tag | Meaning |
|---|---|
| `direct` | `RuntimeHelpers.PrepareMethod` compiled it without running it: every non-virtual method, every override, and every virtual method on a struct or ref struct |
| `invoked` | A newslot virtual method on a class, compiled by calling it on an uninitialized instance with default arguments |
| `thunked` | The same, called through an emitted thunk, because a span-like return or parameter type rules out reflection |
| `virtual?` | A newslot virtual that the harness couldn't call, such as one declared on an abstract class. Its listing is in the dump only when `PrepareMethod` happened to compile it |
| `skip [needs --margs]` | A generic method that no `-MethodArgs` closed |

- A newslot virtual is one with a vtable slot of its own: every interface implementation, implicit or explicit, and every `virtual` member that isn't an override.
- `invoked` and `thunked` methods run until they throw, with null fields and default arguments. Compilation finishes before the first instruction runs, so the exception can't affect the listing, but a side effect that comes before the first null dereference, such as writing a file or setting a static field, does happen during the dump.
- A standalone listing is how the method compiles on its own. That's the point for a method that would otherwise inline into its caller and never be compiled by itself, the case this harness exists for. When a small method normally does inline, though, its caller's listing is the code that runs, so dump the caller when that difference matters. A helper the JIT never inlines, such as an always-throwing local function, compiles the same either way.
- An async or iterator method also gets its compiler-generated state machine's `MoveNext` prepared, and that listing is where its code is. In a Release build an async method's state machine is a struct, so its `MoveNext` is `direct` and doesn't run.

## Why a call stayed a call: `-Inlines`

A listing shows that a call survived, not why. `-Inlines` adds `<dump name>.inlines.txt` beside the dump, with every inlining decision the JIT made while compiling the dumped methods, grouped under each method compiled. The calls it didn't inline also print to the console with the JIT's reason, and the trace moves into `prev/` with its dump, so a refactor that costs an inline shows up as a changed line.

```text
MG.Sonarr.Next.Strings.Wildcard:IsMatch(System.ReadOnlySpan`1<wchar>,System.StringComparison) - 10 inlined, 6 not inlined
  FAIL  MG.Sonarr.Next.Strings.Wildcard:IsMatch(System.ReadOnlySpan`1<wchar>,System.ReadOnlySpan`1<wchar>,System.StringComparison) - too many il bytes
  ok    MG.Sonarr.Next.Strings.Wildcard+State:get_Type()
  FAIL  System.MemoryExtensions:Equals(System.ReadOnlySpan`1<wchar>,System.ReadOnlySpan`1<wchar>,System.StringComparison) - unprofitable inline
  ok    System.ReadOnlySpan`1[System.Char]:Slice(int32,int32)
  FAIL  System.ThrowHelper:ThrowArgumentOutOfRangeException() - does not return
```

- `[in X]` names the inliner when the call sits inside an inlinee rather than in the method being compiled, as in ``FAIL System.Buffers.ArrayPool`1[System.Char]:Return(!0[],bool) [in MG.Sonarr.Next.Buffers.Rent:Return(!!0[],bool)] - target not direct``.
- Parameter types use the runtime's IL names: `int32`, `wchar` for `char`, `unsigned int` for `nuint`, `unsigned int8` for `byte`, and `!!0` and `!0` for a method's and a type's generic parameters.

| Reason | What it means |
|---|---|
| `unprofitable inline` | The profitability heuristic said no. Its score isn't exposed |
| `too many il bytes` | The callee's IL is over the inliner's size limit |
| `does not return` | The callee never returns, such as a throw helper |
| `target not direct` | A virtual or interface call that the JIT didn't devirtualize |
| `target not direct managed` | An indirect call with no method to name, printed as `(unknown target)` |
| `noinline per IL/cached result` | `[MethodImpl(MethodImplOptions.NoInlining)]`, or a never-inline verdict that the runtime cached |

- Tracing doesn't change the generated code: traced and untraced dumps of `Wildcard.IsMatch` were byte-identical, and the trace added a fraction of a second to the run (measured 2026-10-25 on .NET 10.0.13).
- Some JIT intrinsics expand without an inlining decision, so they never appear: `Wildcard+Enumerator.MoveNext` calls `Unsafe.Add` and records no decision at all. Others appear as an ordinary `ok`, as `MemoryMarshal.GetReference` does in the same type's constructor (measured 2026-10-25 on .NET 10.0.13).
- The decisions come from the runtime's JIT-tracing events, keyword `0x1000`, which the harness receives in-process through an `EventListener` and filters on the dump's own `DOTNET_JitDisasm` pattern, so the trace covers the methods the dump lists. The release JIT has no inline dump of its own: it prints nothing for `DOTNET_JitPrintInlinedMethods` (measured 2026-10-25 on .NET 10.0.13). A harness warning that no inlining event arrived means tiering was on, since tier-0 code never inlines, which happens only when the exe runs without the script.
- The trace gives the JIT's verdict, never its score: `unprofitable inline` says neither by how much nor which observation tipped it. That takes a Checked JIT's inline dump.

## Why the harness enters some methods

- `RuntimeHelpers.PrepareMethod` can return success for a newslot virtual on a class without compiling it, and the method then never appears in a `-Summary` either. Measured standalone, it skipped every one, through both overloads, in a sealed class or not, shared instantiation or not. In the harness it compiled some of an abstract cmdlet base class's newslot virtuals, and more of them once the types deriving from that class had loaded, so whether it compiles one depends on runtime state. It compiled every override and every virtual method on a struct or ref struct (measured 2026-10-25 on .NET 10.0.13). So the harness calls a class's newslot virtuals instead, sends everything else through `PrepareMethod`, and tags a newslot virtual it couldn't call `virtual?`.
- `MethodInfo.Invoke` rejects a byref-like return or parameter type before it enters the method, so for one with a span in its signature the harness emits a thunk that passes a default value for every argument, an empty span included, and calls the method through its function pointer. A direct `call` would let the JIT inline a small method into the thunk and leave no listing of the method's own.

## When the dump is empty

A 0-byte dump means `DOTNET_JitDisasm` matched nothing the harness compiled. In order of likelihood: the pattern is too narrow (a `-Pattern` needs its leading `*`), the only methods selected were tagged `virtual?` and `PrepareMethod` didn't compile them, or no method was selected. `-Summary` sets `DOTNET_JitDisasmSummary=1`, which adds one line per JIT-compiled method to the dump and tells a pattern that matched nothing apart from a method that never compiled.

## When compiling isn't enough

Some questions need the method to run, not just compile, such as whether a refactor kept its behavior. In a scratch program, `[UnsafeAccessor]` reaches internal constructors, `RuntimeHelpers.GetUninitializedObject` creates an instance without running a constructor, and an output checksum printed to stderr before and after a change shows whether the change kept the results the same.
