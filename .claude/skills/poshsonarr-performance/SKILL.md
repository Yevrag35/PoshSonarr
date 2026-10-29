---
name: poshsonarr-performance
description: Performance-first, allocation-aware design rules for PoshSonarr's C# code. Covers which code is hot in a cmdlet's run and which is cold, choosing between `SpanStringBuilder`, `RentedBuffer<T>`, `PooledBuffer<T>`, `Rent.Array`, and `ArrayPoolMemoryStream`, the `RefEither` ternary that stack-allocates up to `MAX_STACKALLOC` and rents beyond it, the unchecked members of `SpanUnsafeExtensions` (`Ref`, `Offset`, `UnsafeSlice`, `UnsafeSpan`, `AsUnsafeSpan`, `UnsafeCopy`), `PoolMarshal`, `ArraySliceMarshal`, and `ListMarshal`, migrating raw `Unsafe.Add`, `MemoryMarshal.GetReference`, `GetArrayDataReference`, and `CreateSpan` calls and the sites that keep them, writing span loops that RyuJIT compiles well (hoisted base refs, `nuint` indexes, `MethodImpl` inlining trade-offs), and `ConfigureAwait(false)` and lock-or-concurrent-collection choices. Use it whenever you write or change code that runs per object, per JSON value, per wildcard match, per piped input object, or per request, such as the JSON converters, `Wildcard` and `WildcardSet`, the PowerShell property classes, the query and path builders, and the HTTP handlers, whenever you'd add a `List<T>`, `string.Format`, string concatenation in a loop, or a LINQ chain to that code, size a `stackalloc`, or rent a buffer, whenever you reach for unchecked element access or a `*Marshal` type, and before you replace pooled, span-based, or unsafe code with a simpler allocating equivalent. Skip it for cold code such as connecting, error formatting, argument completers, and the build scripts.
---

# Performance-first design in PoshSonarr

PoshSonarr is written with a standing goal of performance-first, allocation-aware design. Many of its decisions, some of which look unusual or over-engineered at first glance, exist to cut allocations and per-item work.

Assume that an unusual or complex pattern is intentional and performance-motivated unless the code shows otherwise. That doesn't make it immune from criticism.

## What counts as hot

A cmdlet usually spends most of its run waiting on Sonarr, so the code worth optimizing is the code that repeats inside a run:

- converting each value of a JSON response into a model and its PowerShell properties
- matching wildcards against every result
- handling each object piped into `Process`
- once per request, building its path and query string and buffering its body

Connecting is cold: `Connect-SonarrInstance` builds the service provider and the JSON options, reflecting over the model types the first time. So are error formatting, argument completers, and the build scripts. Keep cold code simple.

## Guiding principles

- **Minimize allocations on hot paths.** Prefer pooled buffers, `Span<T>`, `stackalloc`, and ref structs over LINQ chains and temporary lists.
- **Pool rather than allocate.** When the lifetime is scoped, use `Rent.Array<T>` and `Rent.Return<T>` (`src/MG.Sonarr.Next/Buffers/Rent.cs`) instead of `new T[...]`, and `ArrayPoolMemoryStream` (`src/MG.Sonarr.Next/Services/Http/IO/`) instead of `new MemoryStream()`, as the HTTP handlers do to buffer response bodies. `Rent.Return` ignores a null or empty array, and it clears an array whose element type is or holds a reference whatever `clearArray` says, so the pool never keeps a stale object alive. Return arrays through `Rent.Return` rather than `ArrayPool<T>.Shared.Return` to get that. `Rent.Array` asserts a positive length in Debug builds, so handle a zero length before renting, as `RentedBuffer<T>` and `PooledBuffer<T>` do.
- **Prefer ref structs and stack allocation** (`RentedBuffer<T>`, `SpanStringBuilder`) for short-lived, synchronous work. They can't cross an `await`; use `PooledBuffer<T>` there.
- **Unsafe code is allowed and used deliberately.** `src/Directory.Build.props` sets `AllowUnsafeBlocks` for every project, and `[UnsafeAccessor]` stands in for reflection to reach a private member, as `ListMarshal` does for `List<T>`'s fields and `PooledBuffer<T>` does for `Memory<T>`'s internal constructor. That isn't a license for free-floating pointers: wrap a pointer that's passed between methods or types in a struct, as the `FnPtr` structs (`src/MG.Sonarr.Next/Buffers/FnPtr.cs`) wrap managed function pointers.
- **Throughput-aware threading.** The HTTP client and its handlers await with `ConfigureAwait(false)`. The `Send*Request` helpers on `SonarrApiCmdletBase` block the pipeline thread on those calls with `GetAwaiter().GetResult()`, so a continuation that waited to resume on a captured context could deadlock under a host that installs one.
- **Weigh a lock against a concurrent collection.** Before you choose between one `System.Threading.Lock` and a `System.Collections.Concurrent` collection, consider contention, how often the state is touched, and the cost of taking the lock. Finding one pattern or the other in the codebase doesn't make it the right choice for your case. Thread safety matters even in a cmdlet: PowerShell can run the module's cmdlets on several threads at once, as `ForEach-Object -Parallel` and thread jobs do, and every runspace in the process shares the module's static state, the connection's `ServiceProvider` and every static cache included.

## Buffer selection ladder

When you need a temporary buffer:

1. **Synchronous, building a string:** `SpanStringBuilder.Create(length <= MAX_STACKALLOC ? stackalloc char[length] : length)`. It takes the same `RefEither` ternary as step 2, but the builder owns the buffer itself. See the carve-out below.
2. **Synchronous, with a length that's bounded but unknown at compile time:** `RentedBuffer.Rent<T>(length <= MAX_STACKALLOC ? stackalloc T[length] : length)`. The ternary has no natural type, so it's target-typed to `RefEither<Span<T>, int>`, and the buffer is stack-allocated when the length is small and rented from the pool when it's large. Stack-allocate the exact length, not the threshold. `Wildcard`, `CharCollections`, and `ObjectConverter` each declare a private `MAX_STACKALLOC` of 256 elements for the ternary.
3. **Asynchronous, crossing an `await`:** `PooledBuffer<T>` (`src/MG.Sonarr.Next/Buffers/PooledBuffer.cs`). It's a regular value type and `IDisposable`, with `AsSpan()`, `AsMemory()`, and `ClearOnDispose`. Pass it by `ref` or scope it tightly with `using`: it's mutable, so disposing a copy leaves the original pointing at an array that's back in the pool.
4. **Last resort:** `Rent.Array<T>` with `try`/`finally` and `Rent.Return`, only when neither buffer type fits, such as when an API takes ownership of the raw `T[]`, or when the lifetime spans several methods in a way that doesn't compose with `using`.

Each part of the `RefEither` ternary rests on a language rule. A `stackalloc` nested inside another expression, such as an arm of `?:`, is a `Span<T>` and needs no `unsafe` context. `Span<T>` and `int` don't convert to each other, so the conditional has no natural type. Since C# 9, a conditional without one takes its target's type, here the `RefEither<Span<T>, int>` parameter, as long as each arm converts to that type on its own. `RefEither` provides both conversions with an implicit operator from each type argument, and its `allows ref struct` anti-constraints are what let `Span<T>` be a type argument at all. Ref safety then scopes the whole expression to its narrowest arm: the stack arm can't leave the method, so neither can the union nor the buffer or builder that `RentedBuffer.Rent<T>` or `SpanStringBuilder.Create` returns, even on a run that took the `int` arm.

`stackalloc` needs an unmanaged `T`, so a `T` that is or holds a reference has no stack arm. Rent its buffer with `new RentedBuffer<T>(length)` or `RentedBuffer.RentMin<T>(length)`, as `ArrayExtensions.ToNonNullArray` does. Both return an empty buffer for a length of zero and throw `ArgumentOutOfRangeException` for a negative one.

### Carve-out: `SpanStringBuilder` owns its own buffer

When a `SpanStringBuilder` is the buffer's only consumer, don't apply step 2 and hand the result's span to the builder:

```csharp
// Wrong: two owners for one logical buffer.
using RentedBuffer<char> buffer = RentedBuffer.Rent<char>(length <= MAX_STACKALLOC ? stackalloc char[length] : length);
SpanStringBuilder builder = new(buffer.Span);

// Right: one owner.
SpanStringBuilder builder = SpanStringBuilder.Create(length <= MAX_STACKALLOC ? stackalloc char[length] : length);
```

The `SpanStringBuilder(Span<char>)` constructor, which the implicit conversion from `Span<char>` also calls, keeps the span as the builder's buffer without renting. Once the builder outgrows it, `Grow` rents a pooled array of its own, while the outer `using` still holds the first rental until the scope ends. `Create` takes the same `RefEither<Span<char>, int>` and picks the constructor: the stack arm costs nothing, and the `int` arm lets the builder rent at its own size, a power of two of at least 128 characters.

Two cases still call the constructor directly: a fixed compile-time size, as in `new SpanStringBuilder(stackalloc char[256])`, where there's nothing to decide, and a span from another frame, such as a caller's `stackalloc` passed in as a parameter.

`Build()` returns the string and disposes the builder, and `Dispose` resets the builder before it returns its array, so a `using` builder that ends in `Build()` returns its rental once. A `using` local is read-only, though, so it can't be passed as `ref SpanStringBuilder` to an append helper (CS1657), while calls made on it still change the builder itself rather than a copy (measured 2026-10-28 with .NET SDK 10.0.403). Use `using` when the builder is never passed by reference, and `try`/`finally` with `builder.Dispose()` when it is.

## Unchecked access

A `<Type>Marshal` class in an `*.Intrinsics` namespace is that type's unchecked door, the one place that skips a check the type would otherwise make, and a `<Type>Marshal` added for another type follows the same pattern. Spans, arrays, and bare refs are the exception: their unchecked members are C# 14 extension members of `SpanUnsafeExtensions`, in `MG.Sonarr.Next.Extensions`.

`SpanUnsafeExtensions`, `ListMarshal`, and `SpanStringBuilder` are `internal`. `InternalsVisibleTo` opens them to `MG.Sonarr.Next.Shell` and `MG.Sonarr.Next.Shell.Bases`, but only `MG.Sonarr.Next` has `MG.Sonarr.Next.Extensions` as a global using, so a file in either Shell project needs `using MG.Sonarr.Next.Extensions;` to call the extension members.

Every `SpanUnsafeExtensions` member is `AggressiveInlining`, so the inlined code matches the raw form. What you gain is a call site that states its intent and, in Debug builds, a `Debug.Assert` on the range. What you don't gain is safety: these skip the CLR's bounds check rather than replace it, and the caller still owns the guarantee.

### `SpanUnsafeExtensions`

| Receiver | Member | Returns |
|---|---|---|
| `ReadOnlySpan<T>` | `Ref(index = 0)` | `ref readonly T` |
| `ReadOnlySpan<T>` | `UnsafeSlice(start)`, `UnsafeSlice(start, length)` | `ReadOnlySpan<T>` |
| `ReadOnlySpan<T>` | `UnsafeCopy(destination)` | `void` |
| `Span<T>` | `Ref(index = 0)` | `ref T` |
| `Span<T>` | `UnsafeSlice(start)`, `UnsafeSlice(start, length)` | `Span<T>` |
| `T[]` | `Ref(index = 0)` | `ref T` |
| `T[]` | `AsUnsafeSpan(index)`, `AsUnsafeSpan(index, length)` | `Span<T>` |
| `ref T` where `T : struct` | `Offset(index)` | `ref T` |
| `ref T` where `T : struct` | `UnsafeSpan(length)` | `Span<T>` |

The asserts differ in what they accept. `Ref` requires `index < Length`, so it rejects every index of an empty receiver. `UnsafeSlice` and `AsUnsafeSpan` accept a start equal to the length and return an empty span there, as `span[start..]` does. `Offset` and `UnsafeSpan` can only check that the index or length isn't negative, because a bare ref carries no length.

`AsUnsafeSpan` is the one-step form of `array.Ref(index).UnsafeSpan(length)`. Prefer it to that two-step form when the receiver is an array, since it works for any `T` and accepts an index at the end of the array, and prefer it to `array.AsSpan(index, length)` when the caller already owns the range.

**The one asymmetry worth knowing.** `MemoryMarshal.GetReference(ReadOnlySpan<T>)` launders away the read-only-ness and hands back a mutable `ref T`, while the `ReadOnlySpan<T>` overload of `Ref` honors it and returns `ref readonly T`. Moving a site that binds `ref T local = ref ...`, ref-reassigns, passes the ref as `ref` or `out`, or returns `ref T` is therefore a tightening rather than a rename, and it doesn't compile: binding or passing the result as a mutable ref is CS8329, and calling `Offset` on a `ref readonly` local is CS1510 (measured 2026-10-28 with .NET SDK 10.0.403). `Span<T>` and `T[]` both return `ref T`, so neither is ever narrowed.

When such a site does need the mutable ref, typically to reach `Offset`, whose receiver is `ref T`, write the widening out as `Unsafe.AsRef(in span.Ref())` rather than leaving `MemoryMarshal.GetReference` to launder it silently. Both produce the same code. The explicit call acknowledges that this is the riskier spelling, and it still keeps the base ref hoisted out of the loop.

**`UnsafeCopy` isn't `CopyBlockUnaligned`.** It slices the destination to the source's exact length, which leaves `CopyTo` comparing a length against itself; the JIT folds that away, so neither the comparison nor its throw path is emitted. It keeps the GC write barriers and tolerates overlap, so it's correct for every `T`. Rewriting it over `Unsafe.CopyBlockUnaligned` drops the barriers that a `T` holding references needs, and buys nothing in exchange: since .NET 9, the `cpblk` that `CopyBlockUnaligned` emits resolves through `CORINFO_HELP_MEMCPY` to the same `SpanHelpers.Memmove` that `UnsafeCopy` already calls.

### The other three

| Type | Members |
|---|---|
| `PoolMarshal` (`PooledBuffer<T>`) | `UnsafeCreate<T>(minimumSize, useEntireCapacity = false)`, `GetReference<T>(ref readonly buffer)` returning `ref T`, `Resize<T>(ref buffer, newSize)` |
| `ArraySliceMarshal` (`ArraySlice<T>`) | `Create<T>(array, offset, length)`, an `ArraySlice<T>` built without the constructor's validation |
| `ListMarshal` (`List<T>`) | see below |

`PoolMarshal.GetReference` returns a null ref for a default or empty buffer instead of asserting, so check it with `Unsafe.IsNullRef` unless the buffer is known to hold elements. `Resize` only grows: it rents a new array, copies the old one into it, returns the old one to the pool, and sets the buffer's `Length` to its new capacity.

`ListMarshal` reaches into `List<T>`'s private fields through `[UnsafeAccessor]`:

| Member | What it does |
|---|---|
| `AsSpan<T>(list)`, `AsReadOnlySpan<T>(list)` | a span over the list's elements that aliases the backing array |
| `GetArray<T>(list)`, `GetArray<T>(list, out version)` | the backing array as an `ArraySlice<T>` over the populated range |
| `GetRawArray<T>(list)` | the backing array itself, slots past `Count` included |
| `AddRange<T>(list, params ReadOnlySpan<T>)` | a bulk append that skips the enumerable path |
| `AddRange<T, TOther>(list, FnPtr<TOther, T>, params ReadOnlySpan<TOther>)` | the same, converting as it appends |
| `Create<T>(params ReadOnlySpan<T>)`, `CreateWithCapacity<T>(capacity, params ReadOnlySpan<T>)` | a pre-populated list written straight into the backing array |
| `SetCount<T>(list, size)` | writes the internal size field directly |
| `ValidateVersion<T>(list, expectedVersion)` | throws if the list changed since that version |

`AsSpan` aliases where `ToArray()` copies: anything that grows or changes the list invalidates the span, and `SetCount` publishes slots the list never initialized. Neither is a drop-in substitution; both are a decision. `AddRange` grows the list through `List<T>`'s internal `Grow` method, so a .NET release that renames that method surfaces as a `MissingMethodException` on the first call rather than as a compile error.

A separate, `file`-scoped `ListMarshal` in `src/MG.Sonarr.Next/Metadata/MetadataList_T-Marshalling.cs` serves only `MetadataList<T>`.

## Migrating raw forms

No analyzer reports raw forms in this codebase, so migrating one is a hand edit. These raw forms have helper equivalents that behave the same in Release builds:

| Raw form | Becomes |
|---|---|
| `Unsafe.Add(ref MemoryMarshal.GetReference(s), i)` | `s.Ref(i)` |
| `MemoryMarshal.GetReference(s)` | `s.Ref()` |
| `Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(a), i)` | `a.Ref(i)` |
| `MemoryMarshal.GetArrayDataReference(a)` | `a.Ref()` |
| `MemoryMarshal.CreateSpan(ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(a), i), n)` | `a.AsUnsafeSpan(i, n)` |
| `MemoryMarshal.CreateSpan(ref x, n)` | `x.UnsafeSpan(n)` |
| `Unsafe.Add(ref r, i)` | `r.Offset(i)` |

These sites keep the raw form:

- The `nint` and `nuint` overloads of `Unsafe.Add`. `Ref` and `Offset` index with an `int`, and a native-width index is deliberate (see the next section), as in the `nuint` loop of `ListMarshal.AddRange<T, TOther>`.
- An element type that's a reference type. `Offset` and `UnsafeSpan` are constrained to `struct`.
- A `ReadOnlySpan<T>` source feeding a mutable ref. A plain rename doesn't compile, because of the asymmetry above, and when the site needs the mutable ref, the migration is the explicit `Unsafe.AsRef(in span.Ref())`.
- A base ref taken from a receiver that can be empty, as described next.

Nothing flags the last case at compile time, so a sweep walks straight into it. `Ref()` asserts `(uint)index < (uint)Length`, which makes it illegal on an empty receiver, while `ref T first = ref MemoryMarshal.GetArrayDataReference(array)` ahead of a loop that may run zero times is a legal, deliberate idiom: the ref is taken but never dereferenced. The rewrite still behaves the same in Release builds, so only a Debug build that reaches the empty case catches the break.

Resolve it by restructuring the method rather than keeping the raw form. An early return ahead of the allocation earns the unchecked base ref for the rest of the method, puts the degenerate case in control flow, and skips a zero-length allocation:

```csharp
if (count == 0)
	return [];

var items = new Wildcard[count];
ref Wildcard first = ref items.Ref();	// Earned: count > 0 is proven.
```

`SpanStringBuilder.TrimEnd` does the same by taking `_chars.Ref()` only inside `if (_position > 0)`.

Swapping a bounds-checked `Slice` for `UnsafeSlice` is a different kind of change: it drops a check rather than renaming a call. Decide per site whether the range is already guaranteed. That the method already bypasses a check elsewhere doesn't make this one safe to drop.

## Span hot-loop codegen

Choosing the buffer is half the job. The other half is writing the loop so that RyuJIT compiles it well. The recipe below holds for any span-based per-element loop, but it describes one compiler's behavior, which changes between .NET releases, so confirm it with a dump of the method rather than taking it on faith.

Three codegen penalties hide in innocent-looking span loops:

1. On Windows x64, `Span<T>` and `ReadOnlySpan<T>` parameters are passed by reference, so a store through a ref derived from one can force the JIT to assume aliasing and reload the span's data pointer from the parameter on every iteration that derives the ref again.
2. An `int` index inside `Unsafe.Add` can cost a sign extension (`movsxd`) per element when the JIT can't prove the index non-negative.
3. Too many live values in one method make the register allocator spill loop-carried values, such as a base ref that's reloaded from the stack for every element.

The fixes:

- Hoist each span's reference into a `ref` local once, at the top of the method, as in `ref byte destinationBase = ref destination.Ref();`. Below that point, pass helpers a `ref T` and an explicit length, never a span.
- Index with `nuint`, both the loop variable and any lookup-table index. That removes the per-element sign extension, and an `x * 4` byte offset folds into scaled addressing (`[base+4*rx]`). `ListMarshal.AddRange<T, TOther>` hoists both of its refs and walks them with a `nuint` index.
- Form a segment's ref at the point of use from an `int` offset, as in `ref Unsafe.Add(ref base, (nint)(uint)offset)`, and advance the offset with `+= step`, never the ref. That's strength reduction, with no multiply per segment, and no ref ever points past the buffer. This one stays raw: `Offset` takes an `int`, which would bring back the sign extension that the `nint` cast exists to remove.

In the dump, a clean loop body keeps its base refs in registers, with no stack loads of them, no `movsxd` on the index, and no call to `CORINFO_HELP_RNGCHKFAIL`. Seeing any of those means one of the three penalties is still in play.

### Inlining: the counter-intuitive part

Don't fight the inliner on the hot path. The JIT inlines a plain private helper into each call site and specializes its constant arguments, folding a literal `step: 8` into `add reg, 8`. Restructuring around hoisted refs and `nuint` indexes is what relieves register pressure; `NoInlining` isn't.

`NoInlining` on a helper that the hot path calls trades the inlined, specialized copies for a call on every iteration, and the standalone helper can still spill its base refs around its own loop. A smaller total code size isn't a smaller hot footprint when the extra bytes are specializations that never run.

`NoInlining` on a whole cold branch is a different move, and it wins: the hot method shrinks, and its common path falls through instead of jumping over code it never runs. `RentedBuffer.Rent<T>(RefEither<Span<T>, int>)` and `SpanStringBuilder.Create` keep their pool-renting arm in a `NoInlining` local function, so only the stack arm inlines into callers. `ListMarshal.CheckAndGrow` is the out-of-line growth half of `ReserveTail`, so the capacity test inlines into each caller without carrying the growth path, and `SpanStringBuilder.Grow` and the throw helpers make the same move.

Decide which of the two you have before applying either. Extracting a helper that the hot path calls costs per-iteration overhead, while extracting a path that the hot path never takes costs nothing and shrinks the hot footprint.

Never pass a struct local by `ref` to a `NoInlining` callee. Taking its address stops the JIT from promoting the struct's fields to registers anywhere in the method, so the struct gets a stack home, and its fields are stored and reloaded around every use, even on paths that never make the call. Hand the cold helper raw primitives, refs and ints, and let it build its own struct, and declare the hot path's struct inside the branch that uses it so that nothing takes its address. An `AggressiveInlining` callee is exempt: once it's inlined, no call needs the struct's address.

### The best pooled buffer is no buffer

Before working down the ladder, check whether the buffer is needed at all. Iterating the source directly, or computing values as they're consumed, removes the rent-and-return pair, the `try`/`finally` or `using` that guards it, and the bounds checks on the buffer.

## When writing or modifying code

- Before you add a `List<T>`, `string.Format`, string concatenation in a loop, or a LINQ chain to a hot path, consider whether a pooled buffer, a `stackalloc`, or `SpanStringBuilder` fits better.
- Don't "simplify" pooled or span-based code into an allocating equivalent without understanding why the original pattern exists.
- The JSON converters and naming policies under `src/MG.Sonarr.Next/Json/`, `Wildcard` and `WildcardSet`, the PowerShell property classes under `src/MG.Sonarr.Next/PSProperties/`, the query and path builders under `src/MG.Sonarr.Next/Services/Http/`, and the HTTP handlers' pooled body buffering are written in this style. Preserve it when you touch adjacent code.
- Correctness still comes first. Don't trade safety or clarity for micro-optimizations outside hot paths, and save aggressive optimization for code that runs per object, per value, or per request.
- Verify a codegen claim instead of reasoning it out, such as whether a call inlines, whether a bounds check is elided, or whether a restructure changed the emitted code. `pwsh -NoProfile -File src/MG.Sonarr.Tools.Asm/run-asmdump.ps1 <Class> [Method] -DontOpen` builds the solution in Release and writes the RyuJIT FullOpts disassembly of the named methods to `.temp/asm_<Class>[_<Method>].txt`, and `-Inlines` adds the JIT's inlining decisions, with its reason for each call it didn't inline.

## Reference

- `src/MG.Sonarr.Next/Buffers/`: `RentedBuffer<T>` and its static `RentedBuffer` factory, `PooledBuffer<T>`, `Rent`, `SpanStringBuilder`, `ArraySlice<T>`, and the `FnPtr` structs, in `MG.Sonarr.Next.Buffers`
- `src/MG.Sonarr.Next/Buffers/Intrinsics/`: `PoolMarshal` and `ArraySliceMarshal`, in `MG.Sonarr.Next.Buffers.Intrinsics`
- `src/MG.Sonarr.Next/Collections/Intrinsics/ListMarshal.cs`: `ListMarshal`, in `MG.Sonarr.Next.Collections.Intrinsics`
- `src/MG.Sonarr.Next/Extensions/SpanUnsafeExtensions.cs`: the unchecked span, array, and ref members, in `MG.Sonarr.Next.Extensions`
- `src/MG.Sonarr.Next/Services/Http/IO/ArrayPoolMemoryStream.cs`: the pooled `Stream`, in `MG.Sonarr.Next.Services.Http.IO`
- `src/MG.Sonarr.Next/Unions/RefEither_2.cs`: `RefEither<T1, T2>`, the union behind the ladder's ternary, in `MG.Sonarr.Next.Unions`
