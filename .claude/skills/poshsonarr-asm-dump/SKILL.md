---
name: poshsonarr-asm-dump
description: Dump and read the RyuJIT FullOpts disassembly of PoshSonarr methods with `src/MG.Sonarr.Tools.Asm/run-asmdump.ps1`, and the JIT's inlining decisions with `-Inlines`. Covers running it from a session, why it builds in Release, where dumps and their previous baselines land, selecting a method, its overloads, constructors, an accessor or indexer, an explicit interface implementation, a local function, a lambda's display class, a nested type, and generic types and methods (shared `__Canon` code, nullable and constrained type arguments), what the `direct`, `invoked`, `thunked`, and `virtual?` tags mean and which methods the harness runs to compile them, reading the inline trace and its reasons, and why a dump comes back empty. Use it to verify a claim about generated code instead of reasoning it out, such as whether a call inlines and why not, whether a bounds check or a span's reference hoist survives, or whether a refactor changed the emitted code, to compare assembly before and after an edit, to find out why a hot path runs slower than expected, and whenever a dump comes back empty or a method is tagged `virtual?`.
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
- When you measure something about running the harness or reading its output that later work could depend on, describe the finding to the user and ask whether to add it to the reference that covers its area, or to this file when every run can meet it, as with the builds and the output files, and add it only once they agree. Add it with the .NET version and the date, stated as a rule rather than as a story. A finding about how the JIT compiles some shape of code, such as a throw helper or a span loop, is a rule for writing that code, so it doesn't belong in this skill.
- Keep findings out of this skill's description and its list of references, which name only areas and what calls for each reference. The skill listing cuts off a long description, and the triggers at its end go with it.

## References

Read the reference for the area you're working in.

- `references/selecting.md`: what each invocation selects, finding a member's compiled name, the type search and the JIT pattern, closing generic types and methods and reading their shared `__Canon` code, nested types, and local functions and lambdas. Read it before you dump a constructor, a property or indexer, an explicit interface implementation, a generic type or method, a nested type, a local function, a lambda, or a type outside the module's projects, when you don't know a member's exact name, before you pass `-Pattern` or `-Assembly`, before you read a listing of shared `__Canon` code, and when a run ends with exit code 2 or lists types or methods instead of dumping them.
- `references/listings.md`: what the harness's tags mean, which methods it runs to compile them and what running them can do, what a standalone listing shows next to its caller's, where an iterator's or an async method's code is, the two listings of a runtime-async method and how it suspends and resumes, and why a dump comes back empty. Read it before you draw a conclusion from a dump, before you dump an interface or virtual method of a class, which the harness may run, before you dump an iterator or an async method, and whenever a dump comes back empty or a method is tagged `virtual?`.
- `references/inlines.md`: the trace of the JIT's inlining decisions that `-Inlines` writes, how to read its lines, what each reason for not inlining a call means, and what the trace doesn't show. Read it before you pass `-Inlines`, and before you answer whether or why a call inlines.

## Builds

- Each run first builds `PoshSonarr.slnx` in Release, which takes a few seconds and also packages the module into the Shell project's Release output. `-NoBuild` skips the build and dumps the last Release build, so pass it only when nothing under `src/` has changed since.
- It has to be Release. With Debug-built assemblies the JIT compiles their methods at MinOpts even with tiering off, and each listing's first line ends in `(MinOpts)` instead of `(FullOpts)` (measured 2026-10-25 on .NET 10.0.13).
- The build prints nothing unless it fails, and then only its errors.

## Output files

- Dumps land in the repo's `.temp/` folder, which is gitignored, as `.temp/asm_<Class>[_<Method>].txt`. A relative `-OutFile` also resolves under `.temp/`. A nested type's file keeps its outer type's name, as in `asm_PooledBuffer_Enumerator_MoveNext.txt`.
- A successful run first moves the existing dump into `.temp/prev/` under the same name, so the next run of the same command can be compared with it.
- A failed or 0-byte run changes neither file, so after one, the dump file still holds the last good run, not this one. A failed run's partial output, if it wrote any, is in `<dump file>.new`.
- Disassembly goes to the dump. Every line the harness prints itself goes to the console.
