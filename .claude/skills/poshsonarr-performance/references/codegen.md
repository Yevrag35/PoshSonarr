# Span loops and inlining

These rules describe how RyuJIT compiles span loops, the helpers they call, and throw helpers. The loop recipe holds for any span-based per-element loop, but all of it describes one compiler's behavior, which changes between .NET releases, so confirm it with a dump of the method rather than taking it on faith.

Verify every codegen claim that way instead of reasoning it out, such as whether a call inlines, whether a bounds check is elided, or whether a restructure changed the emitted code. `pwsh -NoProfile -File src/MG.Sonarr.Tools.Asm/run-asmdump.ps1 <Class> [Method] -DontOpen` builds the solution in Release and writes the RyuJIT FullOpts disassembly of the named methods to `.temp/asm_<Class>[_<Method>].txt`, and `-Inlines` adds the JIT's inlining decisions, with its reason for each call it didn't inline.

## Span loops

Three codegen penalties hide in innocent-looking span loops:

1. On Windows x64, `Span<T>` and `ReadOnlySpan<T>` parameters are passed by reference, so a store through a ref derived from one can force the JIT to assume aliasing and reload the span's data pointer from the parameter on every iteration that derives the ref again.
2. An `int` index inside `Unsafe.Add` can cost a sign extension (`movsxd`) per element when the JIT can't prove the index non-negative.
3. Too many live values in one method make the register allocator spill loop-carried values, such as a base ref that's reloaded from the stack for every element.

The fixes:

- Hoist each span's reference into a `ref` local once, at the top of the method, as in `ref byte destinationBase = ref destination.Ref();`. Below that point, pass helpers a `ref T` and an explicit length, never a span.
- Index with `nuint`, both the loop variable and any lookup-table index. That removes the per-element sign extension, and an `x * 4` byte offset folds into scaled addressing (`[base+4*rx]`). `ListMarshal.AddRange<T, TOther>` hoists both of its refs and walks them with a `nuint` index.
- Form a segment's ref at the point of use from an `int` offset, as in `ref Unsafe.Add(ref base, (nint)(uint)offset)`, and advance the offset with `+= step`, never the ref. That's strength reduction, with no multiply per segment, and no ref ever points past the buffer. Keep this one a raw `Unsafe.Add` rather than `SpanUnsafeExtensions`' `Offset`, which takes an `int` and would bring back the sign extension that the `nint` cast exists to remove.

In the dump, a clean loop body keeps its base refs in registers, with no stack loads of them, no `movsxd` on the index, and no call to `CORINFO_HELP_RNGCHKFAIL`. Seeing any of those means one of the three penalties is still in play.

## Inlining

Don't fight the inliner on the hot path. The JIT inlines a plain private helper into each call site and specializes its constant arguments, folding a literal `step: 8` into `add reg, 8`. Restructuring around hoisted refs and `nuint` indexes is what relieves register pressure; `NoInlining` isn't.

`NoInlining` on a helper that the hot path calls trades the inlined, specialized copies for a call on every iteration, and the standalone helper can still spill its base refs around its own loop. A smaller total code size isn't a smaller hot footprint when the extra bytes are specializations that never run.

`NoInlining` on a whole cold branch is a different move, and it wins: the hot method shrinks, and its common path falls through instead of jumping over code it never runs. `RentedBuffer.Rent<T>(RefEither<Span<T>, int>)` and `SpanStringBuilder.Create` keep their pool-renting arm in a `NoInlining` local function, so only the stack arm inlines into callers. `ListMarshal.CheckAndGrow` is the out-of-line growth half of `ReserveTail`, so the capacity test inlines into each caller without carrying the growth path, and `SpanStringBuilder.Grow` makes the same move.

Decide which of the two you have before applying either. Extracting a helper that the hot path calls costs per-iteration overhead, while extracting a path that the hot path never takes costs nothing and shrinks the hot footprint.

## Throw helpers

A local function that only throws is the exception to the cold-branch rule: don't mark it `NoInlining` (measured 2026-10-31 on .NET 10.0.14). The JIT finds out that a call never returns by reading the callee's IL when it considers inlining it, which `NoInlining` stops it from doing, and it ignores `[DoesNotReturn]`, which only feeds the C# compiler's flow analysis. Without `NoInlining`, the JIT still doesn't inline a helper that only throws, but it moves the call past the method's `ret` with an `int3` after it, so the common path falls through. With it, the JIT treats the call as one that returns: the call stays on the main path, the common path jumps over it, and the JIT can reload values around it. Writing the call as `return throwX(...)` moves it off the main path even then, but the block pays for its own copy of the epilog. Keep `[DoesNotReturn]` and `[StackTraceHidden]` on the local function, since the compiler and the stack trace still use them.

The rule has two limits, both measured 2026-11-01 on .NET 10.0.14. First, the JIT reads the helper's IL only when it fits under the inliner's size cap, so a helper over the cap, which the inline trace logs as `too many il bytes`, keeps its call on the main path with or without `NoInlining`. Keep a long message out of the helper: build it in a method of its own that returns the string, and leave the helper nothing but that call, the `new`, and the `throw`, as `FlagEnumerator`'s `throwMismatchedSizes` does with `makeNotSupportedMessage`. Second, a guard method that isn't inlined and makes no call but the throw compiles smaller with `NoInlining` on its throw helper. The JIT tail-jumps to a helper that it treats as returning, so the guard needs no stack frame, but a call that never returns, with its `int3`, needs a frame that the fast path pays for too. Judge a guard that inlines by its caller's listing, and keep `NoInlining` only on the throw helper of a guard that runs as a call of its own.

In a generic type, give a throw helper no type parameters and no `out` or `ref` arguments, and keep the constructed type out of the throw path. Shared code, which runs for every reference-type instantiation, pays for each of those on every path, not only on the one that throws: a generic helper or a `typeof(T)` puts a lookup in the generic dictionary ahead of the check, `typeof(T)` and `this.GetType()` make the prologue save the generic context to a stack slot, and a local passed by `out` or `ref` gets a stack slot instead of a register. An instantiation that isn't shared has the type as a constant and its throw after the `ret` either way, so there only the cold bytes differ (measured 2026-11-11 and 2026-11-12 on .NET 11.0.0). Assign the `out` values in the method rather than through the helper, call a non-generic helper that takes no arguments, and let the helper name the generic type definition, as in `typeof(Either<,>)`, when its exception needs a type, as `ObjEither<T1, T2>.TryGetT1`'s default arm and `Either<T1, T2>`'s `EitherThrowHelper.ThrowEmpty` do.

## Struct locals passed by `ref`

Never pass a struct local by `ref` to a `NoInlining` callee. Taking its address stops the JIT from promoting the struct's fields to registers anywhere in the method, so the struct gets a stack home, and its fields are stored and reloaded around every use, even on paths that never make the call. Hand the cold helper raw primitives, refs and ints, and let it build its own struct, and declare the hot path's struct inside the branch that uses it so that nothing takes its address. An `AggressiveInlining` callee is exempt: once it's inlined, no call needs the struct's address.
