# Choosing a buffer

## No buffer first

The best pooled buffer is no buffer. Before you work down the ladder below, check whether you need a buffer at all. Iterating the source directly, or computing values as they're consumed, removes the rent-and-return pair, the `try`/`finally` or `using` that guards it, and the bounds checks on the buffer.

## The ladder

When you do need a temporary buffer:

1. **Synchronous, building a string:** `SpanStringBuilder.Create(length <= MAX_STACKALLOC ? stackalloc char[length] : length)`. It takes the same `RefEither` ternary as step 2, but the builder owns the buffer itself, as "A `SpanStringBuilder` owns its buffer" below describes.
2. **Synchronous, with a length that's bounded but unknown at compile time:** `RentedBuffer.Rent<T>(length <= MAX_STACKALLOC ? stackalloc T[length] : length)`. The ternary has no natural type, so it's target-typed to `RefEither<Span<T>, int>`, and the buffer is stack-allocated when the length is small and rented from the pool when it's large. Stack-allocate the exact length, not the threshold. `ObjectConverter`, `MetadataTag`, and `QueryCol` are among the types that each declare a private `MAX_STACKALLOC` of 256 elements for the ternary.
3. **Asynchronous, crossing an `await`:** `PooledBuffer<T>`. `SpanStringBuilder` and `RentedBuffer<T>` are ref structs, so they can't cross an `await`, while `PooledBuffer<T>` is a regular value type and `IDisposable`, with `AsSpan()`, `AsMemory()`, and `ClearOnDispose`. Pass it by `ref` or scope it tightly with `using`: it's mutable, so disposing a copy leaves the original pointing at an array that's back in the pool.
4. **Last resort:** `Rent.Array<T>` with `try`/`finally` and `Rent.Return`, only when neither buffer type fits, such as when an API takes ownership of the raw `T[]`, or when the lifetime spans several methods in a way that doesn't compose with `using`.

Ref safety scopes the ternary to its narrowest arm: the stack arm can't leave the method, so neither can the buffer or builder that `RentedBuffer.Rent<T>` or `SpanStringBuilder.Create` returns, even on a run that took the `int` arm.

`stackalloc` needs an unmanaged `T`, so a `T` that is or holds a reference has no stack arm. Rent its buffer with `new RentedBuffer<T>(length)` or `RentedBuffer.RentMin<T>(length)`. Both return an empty buffer for a length of zero and throw `ArgumentOutOfRangeException` for a negative one.

## A `SpanStringBuilder` owns its buffer

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

`Build()` returns the string and disposes the builder, and `Dispose` resets the builder before it returns its array, so a `using` builder that ends in `Build()` returns its rental once. A `using` local is read-only, though, so it can't be passed as `ref SpanStringBuilder` to an append helper. Calls made on it still change the builder itself rather than a copy (measured 2026-10-28 with .NET SDK 10.0.403). Use `using` when the builder is never passed by reference, and `try`/`finally` with `builder.Dispose()` when it is.

## Renting arrays and streams

When a buffer's lifetime is scoped, rent it instead of allocating it with `new T[...]` or `new MemoryStream()`. The ladder above picks the wrapper for an array. For a stream, use `ArrayPoolMemoryStream`, as the HTTP handlers do to buffer response bodies.

Return a rented array through `Rent.Return` rather than `ArrayPool<T>.Shared.Return`: it always clears an array that holds references, and in Debug builds it fills an array that it doesn't clear with `0xDD` bytes, so junk such as `'\uDDDD'` characters means that code read an array after returning it. `Rent.Array` asserts a positive length in Debug builds, so handle a zero length before renting, as `RentedBuffer<T>` and `PooledBuffer<T>` do.

## Where the types live

- `src/MG.Sonarr.Next/Buffers/`: `SpanStringBuilder`, `RentedBuffer<T>` and its static `RentedBuffer` factory, `PooledBuffer<T>`, and `Rent`, in `MG.Sonarr.Next.Buffers`
- `src/MG.Sonarr.Next/Unions/RefEither_2.cs`: `RefEither<T1, T2>`, the union behind the ternary, in `MG.Sonarr.Next.Unions`
- `src/MG.Sonarr.Next/Services/Http/IO/ArrayPoolMemoryStream.cs`: the pooled `Stream`, in `MG.Sonarr.Next.Services.Http.IO`

All of them are `internal`, and `InternalsVisibleTo` opens them to `MG.Sonarr.Next.Shell`, `MG.Sonarr.Next.Shell.Bases`, and `MG.Sonarr.Next.Tests`.
