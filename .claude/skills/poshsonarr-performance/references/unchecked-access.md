# Unchecked access

A `<Type>Marshal` class in an `*.Intrinsics` namespace is that type's unchecked door, the one place that skips a check the type would otherwise make, and a `<Type>Marshal` added for another type follows the same pattern. Spans, arrays, and bare refs are the exception: their unchecked members are C# 14 extension members of `SpanUnsafeExtensions`, in `MG.Sonarr.Next.Extensions`.

`SpanUnsafeExtensions` and the `*Marshal` types are `internal`. `InternalsVisibleTo` opens them to `MG.Sonarr.Next.Shell`, `MG.Sonarr.Next.Shell.Bases`, and `MG.Sonarr.Next.Tests`, but only `MG.Sonarr.Next` has `MG.Sonarr.Next.Extensions` as a global using, so a file in any of those three projects needs `using MG.Sonarr.Next.Extensions;` to call the extension members.

## `SpanUnsafeExtensions`

Every `SpanUnsafeExtensions` member is `AggressiveInlining`, so the inlined code matches the raw `Unsafe` and `MemoryMarshal` form. What you gain is a call site that states its intent and, in Debug builds, a `Debug.Assert` on the range. What you don't gain is safety: these skip the CLR's bounds check rather than replace it, and the caller still owns the guarantee. Each member is marked `unsafe`, so every call sits in an `unsafe` block whose `// SAFETY:` comment says why the call is valid there.

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

**The one asymmetry worth knowing.** `MemoryMarshal.GetReference(ReadOnlySpan<T>)` launders away the read-only-ness and hands back a mutable `ref T`, while the `ReadOnlySpan<T>` overload of `Ref` honors it and returns `ref readonly T`. Moving a site that binds `ref T local = ref ...`, ref-reassigns, passes the ref as `ref` or `out`, calls `Offset` on it, or returns `ref T` is therefore a tightening rather than a rename, and it doesn't compile. `Span<T>` and `T[]` both return `ref T`, so neither is ever narrowed.

When such a site does need the mutable ref, typically to reach `Offset`, whose receiver is `ref T`, write the widening out as `Unsafe.AsRef(in span.Ref())` rather than leaving `MemoryMarshal.GetReference` to launder it silently. Both produce the same code. The explicit call acknowledges that this is the riskier spelling, and it still keeps the base ref hoisted out of the loop.

## The `*Marshal` types

| Type | Members |
|---|---|
| `PoolMarshal` (`PooledBuffer<T>`) | `UnsafeCreate<T>(minimumSize, useEntireCapacity = false)`, `GetReference<T>(ref readonly buffer)` returning `ref T`, `Resize<T>(ref buffer, newSize)` |
| `ArraySliceMarshal` (`ArraySlice<T>`) | `Create<T>(array, offset, length)`, an `ArraySlice<T>` built without the constructor's validation |
| `ListMarshal` (`List<T>`) | see below |

`PoolMarshal.GetReference` returns a null ref for a default or empty buffer instead of asserting, so check it with `Unsafe.IsNullRef` unless the buffer is known to hold elements.

`ListMarshal` reaches into `List<T>`'s private fields through `[UnsafeAccessor]` for spans over a list's elements (`AsSpan`, `AsReadOnlySpan`), its backing array (`GetArray`, `GetRawArray`), bulk appends that can convert as they go (`AddRange`), lists written straight into their backing array (`Create`, `CreateWithCapacity`), and its count and version (`SetCount`, `ValidateVersion`). `AsSpan` aliases where `ToArray()` copies: anything that grows or changes the list invalidates the span, and `SetCount` publishes slots the list never initialized. Neither is a drop-in substitution; both are a decision.

## Migrating raw forms

No analyzer reports raw forms in this codebase, so migrating one is a hand edit. The raw forms and the helpers are all marked `unsafe`, so a migration keeps the site's `unsafe` block and its `// SAFETY:` comment. These raw forms have helper equivalents that behave the same in Release builds:

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

- The `nint` and `nuint` overloads of `Unsafe.Add`. `Ref` and `Offset` index with an `int`, which can cost a loop a sign extension per element, so a native-width index is deliberate, as in the `nuint` loop of `ListMarshal.AddRange<T, TOther>`.
- An element type that's a reference type. `Offset` and `UnsafeSpan` are constrained to `struct`.
- A `ReadOnlySpan<T>` source feeding a mutable ref. A plain rename doesn't compile, because of the asymmetry above, and when the site needs the mutable ref, the migration is the explicit `Unsafe.AsRef(in span.Ref())`.
- A base ref taken from a receiver that can be empty, as described next.

Nothing flags the last case at compile time, so a sweep walks straight into it. `ref T first = ref MemoryMarshal.GetArrayDataReference(array)` ahead of a loop that may run zero times is a legal, deliberate idiom: the ref is taken but never dereferenced. `Ref()`'s assert rejects that empty receiver, though, while the rewrite behaves the same in Release builds, so only a Debug build that reaches the empty case catches the break.

Resolve it by restructuring the method rather than keeping the raw form. An early return ahead of the allocation earns the unchecked base ref for the rest of the method, puts the degenerate case in control flow, and skips a zero-length allocation:

```csharp
if (count == 0)
	return [];

T[] items = new T[count];
unsafe
{
	// SAFETY: The early return leaves count > 0, so items has a first element.
	ref T first = ref items.Ref();
	// The loop over items goes here, inside the block, so that first stays in scope.
}
```

`SpanStringBuilder.TrimEnd` does the same by taking `_chars.Ref()` only inside `if (_position > 0)`.

Swapping a bounds-checked `Slice` for `UnsafeSlice` is a different kind of change: it drops a check rather than renaming a call. Decide per site whether the range is already guaranteed. That the method already bypasses a check elsewhere doesn't make this one safe to drop.

## Where the types live

- `src/MG.Sonarr.Next/Extensions/SpanUnsafeExtensions.cs`: the unchecked span, array, and ref members, in `MG.Sonarr.Next.Extensions`
- `src/MG.Sonarr.Next/Buffers/Intrinsics/`: `PoolMarshal` and `ArraySliceMarshal`, in `MG.Sonarr.Next.Buffers.Intrinsics`
- `src/MG.Sonarr.Next/Collections/Intrinsics/ListMarshal.cs`: `ListMarshal`, in `MG.Sonarr.Next.Collections.Intrinsics`
- `src/MG.Sonarr.Next/Buffers/`: `PooledBuffer<T>` and `ArraySlice<T>`, the types that `PoolMarshal` and `ArraySliceMarshal` open, in `MG.Sonarr.Next.Buffers`
