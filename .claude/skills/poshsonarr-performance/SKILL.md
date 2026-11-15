---
name: poshsonarr-performance
description: Performance-first, allocation-aware design rules for PoshSonarr's C# code. Covers which code is hot and which is cold; choosing a buffer (`SpanStringBuilder`, `RentedBuffer<T>`, `PooledBuffer<T>`, `Rent.Array`, `ArrayPoolMemoryStream`, and the `MAX_STACKALLOC` ternary); unchecked access through `SpanUnsafeExtensions` and the `*Marshal` types instead of raw `Unsafe` and `MemoryMarshal` calls; `unsafe` marks, `// SAFETY:` comments, and `/// <safety>` contracts; span loops and inlining that RyuJIT compiles well; and `ConfigureAwait(false)` and lock-or-concurrent-collection choices. Use it whenever you write or change code that runs per object, per JSON value, per wildcard match, per piped input object, or per request, such as the JSON converters, `Wildcard` and `WildcardSet`, the PowerShell property classes, the query and path builders, and the HTTP handlers, whenever you'd add a `List<T>`, `string.Format`, string concatenation in a loop, or a LINQ chain to that code, size a `stackalloc`, or rent a buffer, whenever you reach for unchecked element access or a `*Marshal` type or write an `unsafe` block, and before you replace pooled, span-based, or unsafe code with a simpler allocating equivalent. Skip it for cold code such as connecting, error formatting, argument completers, and the build scripts.
---

# Performance-first design in PoshSonarr

PoshSonarr is written with a standing goal of performance-first, allocation-aware design. Many of its decisions, some of which look unusual or over-engineered at first glance, exist to cut allocations and per-item work.

Assume that an unusual or complex pattern is intentional and performance-motivated unless the code shows otherwise. That doesn't make it immune from criticism.

## What counts as hot

A cmdlet usually spends most of its run waiting on Sonarr, so the code worth optimizing is the code that repeats inside a run:

- converting each value of a JSON response into a model and its PowerShell properties, in the converters and naming policies under `src/MG.Sonarr.Next/Json/` and the property classes under `src/MG.Sonarr.Next/PSProperties/`
- matching wildcards against every result, in `Wildcard` and `WildcardSet`
- handling each object piped into `Process`
- once per request, building its path and query string and buffering its body, in the query and path builders and the HTTP handlers under `src/MG.Sonarr.Next/Services/Http/`

That code is written in the style this skill describes, so preserve the style when you touch the code around it. There, prefer pooled buffers, `Span<T>`, `stackalloc`, `SpanStringBuilder`, and ref structs over LINQ chains, temporary lists, `string.Format`, and string concatenation in a loop.

Connecting is cold: `Connect-SonarrInstance` builds the service provider and the JSON options. So are error formatting, argument completers, and the build scripts. Keep cold code simple and safe, and never trade correctness for speed, in hot code or cold.

## References

Read the reference for each area that your work touches. Each one stands alone.

- `references/buffers.md`: whether a temporary buffer is needed at all, which buffer fits synchronous, string-building, and asynchronous work, the `MAX_STACKALLOC` ternary, buffers of reference types, how a `SpanStringBuilder` owns and returns its buffer, and renting and returning arrays and streams. Read it before you allocate an array, a list, a stream, or a string in hot code, size a `stackalloc`, rent or return a buffer, create a `SpanStringBuilder` or pass one to a helper, or replace pooled or span-based code with an allocating equivalent.
- `references/unchecked-access.md`: `SpanUnsafeExtensions` and the `*Marshal` types, which projects can call them, what their Debug asserts accept, how their refs into a read-only span differ from `MemoryMarshal`'s, migrating raw `Unsafe` and `MemoryMarshal` calls and the sites that keep them, and dropping a bounds check. Read it before you reach for unchecked element access, a `*Marshal` type, or a raw `Unsafe` or `MemoryMarshal` call, add a `*Marshal` type or a member to one, migrate raw calls to the helpers, or swap a bounds-checked slice for an unchecked one.
- `references/memory-safety.md`: what C#'s updated memory safety rules require, which of the module's members are marked `unsafe` and what a `/// <safety>` section states, what a `// SAFETY:` comment covers and how far an `unsafe` block reaches, `[UnsafeAccessor]`, and pointers passed between methods. Read it before you write an `unsafe` block or its `// SAFETY:` comment, mark a member `unsafe` or take the mark off, write a `<safety>` section, change a check or a guard that a `// SAFETY:` comment cites, reach a private member of another type, or pass a pointer between methods or types.
- `references/codegen.md`: checking the code that the JIT generates, the penalties that span loops hide and their fixes, when inlining helps and when `NoInlining` pays, throw helpers and guards, throw helpers in generic types, and struct locals passed by `ref`. Read it before you write or restructure a per-element loop over a span or an array, add or remove `AggressiveInlining` or `NoInlining`, move hot-path code into a helper, write a throw helper or a guard, pass a struct local by `ref` to a helper, or claim anything about the code that the JIT generates.
- `references/threading.md`: `ConfigureAwait(false)` in the HTTP client and its handlers, and choosing between a lock and a concurrent collection for shared state. Read it before you add or change an `await` in the HTTP client or its handlers, block on a task, or add or change static or shared state.

## Changing this skill

- Each reference stands alone, because a session reads only the references for its work, so never point one reference at another. State a fact that two references need in each of them, and put one that every reference needs in this file.
- Keep findings out of this skill's description and its list of references, which name only areas and the changes that call for each reference. The skill listing cuts off a long description, and the triggers at its end go with it.
- When you measure something that later work could depend on, such as how the JIT compiles some shape of code, describe the finding to the user and ask whether to add it to the reference it belongs to, and add it only once they agree. Add it with the .NET version and the date, stated as a rule for writing the code rather than as a story.
