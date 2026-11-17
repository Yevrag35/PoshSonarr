# Why a call stayed a call: `-Inlines`

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
| `unprofitable inline` | The profitability heuristic said no. Its score, and what tipped it, show only in a Checked JIT's inline dump |
| `too many il bytes` | The callee's IL is over the inliner's size limit |
| `does not return` | The callee never returns, such as a throw helper |
| `target not direct` | A virtual or interface call that the JIT didn't devirtualize |
| `target not direct managed` | An indirect call with no method to name, printed as `(unknown target)` |
| `noinline per IL/cached result` | `[MethodImpl(MethodImplOptions.NoInlining)]`, or a never-inline verdict that the runtime cached |

- In shared generic code, listed with `System.__Canon`, a call whose target depends on a type argument names no target in the trace: in `CollectionExtensions.AddMany` with `-MethodArgs string`, the interface call to `ICollection<T>.Add` logs as `(unknown target) - target not direct` (measured 2026-11-03 on .NET 10.0.14).
- A `FAIL` line doesn't show that the call is in the listing. The JIT decides inlines before it removes the branches that inlining made dead, so a throw helper on such a branch keeps its `does not return` line after the branch is gone. `RentedBuffer<T>.Resize` logs `ThrowHelper:ThrowArgumentException_DestinationTooShort()` as `does not return` for both `char` and `__Canon`, but neither listing calls it, because `UnsafeCopy` slices the destination to the source's length and the JIT then removes `CopyTo`'s length check (measured 2026-10-28 on .NET 10.0.14). Search the listing for the callee to tell whether a call survived.
- The reason doesn't show whether the JIT treats a call as never returning: only the listing does, with an `int3` right after the call. A throw helper that gets that treatment logs `does not return` or `unprofitable inline`, depending on its IL size. One marked `NoInlining` never gets it, and neither does one over the inliner's size cap, which logs `too many il bytes` (measured 2026-11-01 on .NET 10.0.14).
- Tracing doesn't change the generated code: traced and untraced dumps of `Wildcard.IsMatch` were byte-identical (measured 2026-10-25 on .NET 10.0.13).
- Some JIT intrinsics expand without an inlining decision, so they never appear, while others appear as an ordinary `ok`. `SpanUnsafeExtensions.Ref` with `-MethodArgs char` calls both `Unsafe.Add` and `MemoryMarshal.GetReference`, and its trace has an `ok` line for `GetReference` and none for `Add` (measured 2026-11-13 on .NET 11.0.0).
- With `<Features>runtime-async=on</Features>` in the build, an async method that returns `Task` or `Task<T>` has two listings under its name: a thunk that the runtime generates, and the async variant that holds the method's code. The trace puts both listings' decisions under one header, the thunk's first, because the two share a name and parameters. The call to `AsyncHelpers.FinishSuspensionNoContinuationContext` that a suspension at an await with `ConfigureAwait(false)` makes gets no line.
- The trace comes from the runtime's JIT-tracing events, filtered on the dump's own `DOTNET_JitDisasm` pattern, so it covers the methods the dump lists. The release JIT has no inline dump of its own: it prints nothing for `DOTNET_JitPrintInlinedMethods` (measured 2026-10-25 on .NET 10.0.13). A harness warning that no inlining event arrived means tiering was on, since tier-0 code never inlines, which happens only when the exe runs without the script.
