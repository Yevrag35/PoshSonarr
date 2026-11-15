# Unsafe code and safety contracts

Unsafe code is allowed and used deliberately. `src/Directory.Build.props` sets `AllowUnsafeBlocks` for every project and opts in to C#'s updated memory safety rules with the `updated-memory-safety-rules` feature, so every unchecked access sits in an `unsafe` block that says why it's valid.

## What the rules require

Under the updated rules, `unsafe` on a member means that its callers need an unsafe context, and it no longer gives the member's body one. A call to a member marked `unsafe` compiles only inside an `unsafe { }` block, and so do a call through a function pointer and a `stackalloc` without an initializer in a `[SkipLocalsInit]` method. A block emits no IL. The .NET 11 reference assemblies, which the compiler checks against, mark most of `Unsafe` and `MemoryMarshal` as well, and the build reports every call that needs a block.

## Which members are marked

The module follows the same rule as those marks: a member is marked `unsafe` only when a caller can corrupt memory through it, as with an index, a range, a pointer, or a type argument that it can't check, and not when the worst a caller can do is break an invariant. A member marked `unsafe` states what its callers must guarantee in a `/// <safety>` section, after `<remarks>` and before `<typeparam>`, and its body's block cites that contract. Any other member that makes an unchecked call keeps a safe signature and justifies the block inside it.

| Marked `unsafe` | Why |
|---|---|
| Every `SpanUnsafeExtensions` member | The index, range, or destination length goes unchecked. |
| `ArraySliceMarshal.Create` and the unchecked `ArraySlice<T>` constructor behind it | The slice reads its elements without bounds checks, trusting the offset and length for as long as it exists. |
| `ListMarshal.SetCount` and both `ListMarshal.GetArray` overloads | Every span and slice over the list trusts its count, and `GetArray`'s slice keeps trusting the count it read. |
| Private helpers whose callers meet a precondition, such as `SpanStringBuilder.AppendCore(ReadOnlySpan<char>)`, which needs the room that `EnsureCapacity` makes | The caller, not the helper, establishes the range. |

`PoolMarshal`'s members, the rest of `ListMarshal`, and the `View<T>` accessors stay safe: none of them can reach past an array on its own, and the `ListMarshal` members that build a span justify it with `List<T>`'s invariant that its count lies within its backing array, which holds while no other thread changes the list.

## `// SAFETY:` comments

Every `unsafe` block starts with a `// SAFETY:` comment that says why each unchecked call in it is valid at that spot: which check, guard, or invariant keeps the index or range inside the memory, or which contract the caller met. A block ends the scope of the locals that it declares, so a block that binds a ref local, as a hoisted base ref, covers every use of it, and the comment covers every call in the block. Where a block would end a local's scope too early, declare the local before the block and assign it inside, as `SpanStringBuilder.AppendSpan` does. Nothing checks the comments or the `<safety>` sections, so they're the record of why each unchecked access is valid, and a change to a guard means rereading the comment that cites it.

## Private members and pointers

`[UnsafeAccessor]` stands in for reflection to reach a private member, as `ListMarshal` does for `List<T>`'s fields and `PooledBuffer<T>` does for `Memory<T>`'s internal constructor. That isn't a license for free-floating pointers: wrap a pointer that's passed between methods or types in a struct, as the `FnPtr` structs (`src/MG.Sonarr.Next/Buffers/FnPtr.cs`) wrap managed function pointers.
