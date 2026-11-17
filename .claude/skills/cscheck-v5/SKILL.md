---
name: cscheck-v5
description: CsCheck 5.x, the C# property-based testing library, renamed, moved, and removed members and changed generator and `Sample` behavior that your training data knows only in their 4.x form. Covers finding which major version a project restores, `Gen.OneOf` and `Gen.Frequency` with constants (4.x's `OneOfConst` and `FrequencyConst`), `.Array`, `.List`, `.HashSet`, `.ArrayUnique`, and `.Array2D` as extension properties that need `using CsCheck;`, unranged integer generators that return `MinValue` and `MaxValue` on about 1 draw in 64, the removed `Gen.UInt4` to `Gen.UInt2048` and the retyped `Gen.UInt128`, `Sample` threads that keep the caller's culture and `AsyncLocal` values, a test framework's context and cancellation token among them, failure reports that hold the property's exception in `CsCheckException.InnerException`, reproducing a failure from its seed, `Check` settings and `CsCheck_*` variables that throw when out of range, and removed members such as `Check.ModelEqual`, `Dbg`, `ILogger`, and the regression helpers. Use it whenever you write, change, review, or debug C# code that uses CsCheck (`Gen.`, `.Sample(`, `Check.`, `CsCheckException`, `CsCheck_Seed`) in a project on CsCheck 5.x, when CsCheck code fails to compile with CS0117, CS0103, CS0246, CS0619, or CS1061, when a property test fails or every CsCheck call fails in the `CsCheck.Check` type initializer, and when you upgrade a project from CsCheck 4.x. Skip it for a project on CsCheck 4.x or earlier.
---

# CsCheck 5.x

Your training data most likely covers CsCheck only through its 4.x releases. CsCheck 5.0 renamed, moved, and removed members and changed what several generators and `Sample` do, so CsCheck code written from memory can fail to compile against 5.x, or compile and then behave differently. Each section gives the 5.x form of a pattern, what 4.x did, and why the difference matters. The last section lists what didn't change, where your memory still holds.

The measurements were taken on 2026-11-07 with CsCheck 4.9.1 and 5.0.1 on .NET 10.0.14 and 8 logical CPUs.

## Check the version first

- Find the CsCheck version that the project restores: the `Version` on its `PackageReference`, the `PackageVersion` item in `Directory.Packages.props` when the repository manages versions centrally, or the `CsCheck/<version>` key under `libraries` in the project's `obj/project.assets.json`. On 4.x, your memory is right and the rest of this skill doesn't apply.
- CsCheck 5.x needs `net10.0` or later. A test project on an earlier framework stays on 4.9.1, the last release for `net8.0`.
- For a member that this skill doesn't cover, check the package rather than your memory. It ships its XML documentation as `cscheck/<version>/lib/<framework>/CsCheck.xml` in the NuGet global packages folder, which `dotnet nuget locals global-packages --list` prints, and a member ID such as `M:CsCheck.Gen.OneOf` finds its entry. A build is quicker still: each section names the compiler error that the 4.x form gets.

## Constants: `Gen.OneOf` and `Gen.Frequency`

`Gen.OneOfConst` and `Gen.FrequencyConst` are gone, and each fails with CS0117. Their constants forms are overloads of `Gen.OneOf` and `Gen.Frequency` now:

| 4.x | 5.x |
| --- | --- |
| `Gen.OneOfConst("a", "b", "c")` | `Gen.OneOf("a", "b", "c")` |
| `Gen.FrequencyConst((1, "a"), (3, "b"))` | `Gen.Frequency((1, "a"), (3, "b"))` |

- Arguments that are generators still bind to the generator overloads. `Gen.OneOf(Gen.Int[0, 4], Gen.Int[10, 14])` and `Gen.OneOf(Gen.Const(1), Gen.Int[5, 9])` are each a `Gen<int>` that picks one of its generators for every draw, and `Gen.Frequency((1, Gen.Const("a")), (3, Gen.Const("b")))` is a `Gen<string>`.
- The constants overloads draw as the 4.x methods did: each constant equally often, or in proportion to its weight.

A pool of units joined into strings keeps a property's inputs on the characters that matter:

```csharp
private static readonly Gen<string> s_text = Gen.OneOf("*", "?", "a", "A", "é", "\U00010400")
	.Array[0, 8]
	.Select(units => string.Concat(units));
```

## Collection generators need the `CsCheck` namespace

`Array`, `List`, `HashSet`, `ArrayUnique`, and `Array2D` are C# 14 extension properties of `Gen<T>`, declared in `CsCheck.GenExtensions`, where 4.x declared them on `Gen<T>` itself. Their indexers didn't change: `.Array[n]` makes arrays of length `n`, and `.Array[min, max]` includes both ends.

- An extension property resolves only where its namespace is imported, by `using CsCheck;` or a global using. Without one, `CsCheck.Gen.Int.Array[1, 3]` fails with CS1061, "'GenInt' does not contain a definition for 'Array'", even though it names the type in full.
- An assembly compiled against 4.x, such as a shared library of generators, has to be rebuilt against 5.x, because these properties moved and the `Check` settings changed from fields to properties.

## Unranged integer generators produce their extremes

On 5.0.1, each of these generators returned an extreme on about 1 draw in 64, over 640,000 draws each:

- `Gen.Int`, `Gen.Long`, `Gen.Short`, and `Gen.Int128`, with `MinValue` or `MaxValue`.
- `Gen.UInt`, `Gen.ULong`, and `Gen.UInt128`, and `Gen.Int.Positive` and `Gen.Int.NonNegative`, with `MaxValue`.

The 4.9.1 versions of `Gen.Int`, `Gen.Long`, `Gen.Short`, `Gen.ULong`, `Gen.Int.Positive`, and `Gen.Int.NonNegative` returned none. These don't favor their extremes in 5.x: `Gen.Byte`, `Gen.SByte`, `Gen.UShort`, `Gen.Int.Uniform`, and ranges, `Gen.Int[int.MinValue, int.MaxValue]` and `Gen.Long[long.MinValue, long.MaxValue]` included.

- A property that does arithmetic on one of these values, such as `x + 1`, `-x`, `Math.Abs(x)`, or a sum of several, now gets inputs that overflow, which 4.x never generated. Finding those boundary bugs is what the change is for, so fix the code, or narrow the generator to the values that the code is meant to take.
- To keep values off the extremes, draw from a range, such as `Gen.Int[1, 1_000_000]` for an ID, use `Gen.Int.Uniform`, or filter with `.Where(x => x != int.MaxValue)`.

## Removed range generators: `Gen.UInt4` to `Gen.UInt2048`

In 4.x, `Gen.UInt4`, `UInt8`, `UInt16`, `UInt32`, `UInt64`, `UInt256`, `UInt512`, `UInt1024`, and `UInt2048` were `Gen<uint>` generators of 0 to N − 1, not byte, ushort, or ulong generators: `Gen.UInt16` drew 0 to 15. 5.x removed them, and each fails with CS0117.

- For the same values, write `Gen.UInt[0, N - 1]`, such as `Gen.UInt[0, 15]`.
- For every value of a type, use `Gen.Byte`, `Gen.UShort`, `Gen.UInt`, or `Gen.ULong`.
- `Gen.UInt128` was a `Gen<uint>` too, of 0 to 127. In 5.x it's a `Gen<UInt128>` over the whole type, so code written for the old values can still compile where `var` hides the type, and then draw far larger numbers. For the old values, write `Gen.UInt[0, 127]`.

## `Sample` threads keep the caller's context

`Sample` runs a property's iterations on the calling thread and on others at the same time, one thread per logical CPU unless `threads:` says otherwise, so a property still has to be safe to run on several threads at once. What changed is the context that those threads run in.

- In 5.x, every iteration runs in the caller's execution context, with its `CultureInfo.CurrentCulture` and its `AsyncLocal` values. xUnit.net v3 keeps `TestContext.Current` in one, so a property sees the test's own context and its `CancellationToken`.
- In 4.x, only the iterations on the calling thread kept them. The others ran under the machine's culture with every `AsyncLocal` empty: 4,651 of 20,000 iterations in a console run, and about half of them in an xUnit.net v3 test.

So under 5.x:

- A test that runs under another culture checks every case of its properties under that culture. xUnit.net v3's `--culture tr-TR`, for one, sets only the test's culture, not `CultureInfo.DefaultThreadCurrentCulture`, so under 4.x the cases on the other threads ran under the machine's culture. Culture bugs that 4.x let through can fail now.
- A property can read `TestContext.Current` and pass `TestContext.Current.CancellationToken`, as the test method does. Copying the token to a local before calling `Sample` works too.

## Failure reports

When a property fails, `Sample` shrinks the failing case and throws a `CsCheckException` whose message holds a seed line and the shrunk value, formatted by `print:`:

```text
Set seed: "<seed>" or -e CsCheck_Seed=<seed> to reproduce (9 shrinks, 5,056 skipped, 10,000 total).
i = 500
```

- The exception that the property threw, such as a failed assertion, is the `CsCheckException`'s `InnerException`, and its message is no longer added to the `CsCheckException`'s. In 4.x, `InnerException` was null and the message ended with the inner exception's message. Code that looks for the failing assertion in `CsCheckException.Message`, such as a helper that rewraps the exception or a test that checks a failing property's message, reads `InnerException` instead.
- A test runner that prints inner exceptions still shows the assertion. xUnit.net v3 puts it on a line that starts with `---- `, and its stack trace under `----- Inner Stack Trace -----`.
- The `CsCheckException`'s stack trace starts at the `Sample` call, and the property's own frames are in the inner exception's trace. In 4.x, the trace started inside the property, or was empty when a predicate returned `false`.
- A predicate that returns `false` fails without an inner exception, in both versions.
- `skipped` counts the candidates that shrinking passed over because they weren't smaller than the smallest failure so far. It doesn't count inputs that failed or that a `Where` filtered out.

## Reproducing a failure

- Pass the seed to the failing call, `seed: "<seed>"`, with `iter: 1, threads: 1` to run only the failing case, or set the `CsCheck_Seed` environment variable, which `dotnet test` sets with `-e CsCheck_Seed=<seed>`. The variable seeds the first iteration of every `Sample` in the process, so run only the failing test with it. Both ways work as they did in 4.x.
- A seed from 4.x still parses, but `Gen.Double`, `Gen.Float`, and `Gen.Decimal` can draw different values from it, and so can anything built from them or from a draw that 5.x turns into an extreme. Record a new seed under 5.x rather than replaying one from 4.x.

## Settings

- The `Check` settings, such as `Check.Iter`, `Check.Threads`, `Check.Seed`, and `Check.Time`, are properties in 5.x, where 4.x had fields. Source that assigns them still compiles.
- 5.x rejects a setting that's out of range. Assigning one, such as `Check.Iter = 0`, throws a `CsCheckException`: "Check.Iter (CsCheck_Iter) must be at least 1, but was 0." A bad `CsCheck_*` environment variable, out of range or not a number, makes the `CsCheck.Check` type's static constructor throw, so every CsCheck call in the process fails with a `TypeInitializationException`, "The type initializer for 'CsCheck.Check' threw an exception", around a `CsCheckException` that names the variable. When every property in a run fails that way, check the environment.
- In 4.x, `CsCheck_Iter=0` made every `Sample` without an `iter:` argument pass without running anything, and `CsCheck_Threads=0` made every `Sample` without a `threads:` argument hang. A value that wasn't a number threw in 4.x too.
- Arguments aren't checked the way settings are. A `threads:` argument below 1 means the default, where `threads: 0` hung in 4.x, and `iter: 0` runs no iterations in either version, so the property passes without checking anything.
- `Check.SingleLimit`, set by `CsCheck_SingleLimit`, is new: `Single` throws after that many tries, 1,000,000 by default.

## Other removed and changed members

| 4.x | 5.x |
| --- | --- |
| `Check.ModelEqual` | Removed, so it fails with CS0117. Model-based and parallel checks compare with `Check.Equal` by default, which counts duplicates in bags. |
| `Dbg` | Removed, with nothing in its place. It fails with CS0103. |
| `ILogger` | Renamed `ISampleLogger`, and the old name fails with CS0246. `Logging.LogProcessor` and `Logging.CreateLogger` are removed. |
| `IRegression`, `RegressionExtensions` | Removed. `Hash.Add` takes collections, such as an `IEnumerable<double>`, as well as single values. |
| `GenOperationAsync<T>` | Removed. `Gen.Operation` with an async lambda fails with CS0619, because `SampleParallel` runs operations synchronously. Wait for the task inside a synchronous operation instead. |
| `new Hash(...)`, `Classifier`, `MedianEstimator`, `HashHelper`, `ThrowHelper`, and the `Timer` types | No longer public. |
| `Gen.SelectMany` | Its selector can return an `IGen<R>` as well as a `Gen<R>`. |
| `Gen.Select` | Takes up to 16 generators with a selector, where 4.x stopped at 8. |
| `Faster`, `FasterAsync` | Take a `warmUp` argument that, before measuring, calls both functions until the JIT has finished optimizing them, for up to 5 seconds. Its default is `Check.WarmUp`, set by `CsCheck_WarmUp`, which is false. |
| `Spec` checks | Return `SpecReport<S>`. `Check.Exhaustive` takes `writeLine` right after the spec, `Spec.AtMost` takes its array of values before `times`, and `SampleFaults` reports `FaultOutcome.NotFound` for a fault that it misses. |

## Unchanged

Code recalled from 4.x for these still compiles and behaves the same:

- `Sample`'s overloads and parameters: an `Action`, a `Func<…, bool>`, or a `Func<…, string>` over up to 8 tuple items, with `writeLine`, `seed`, `iter`, `time`, `threads`, `print`, and `logger`, whose type is `ISampleLogger` now. `print` takes the whole tuple, as in `Gen.Select(a, b).Sample((x, y) => x <= y, print: t => $"{t.Item1}, {t.Item2}")`.
- The defaults of 100 iterations and one thread per logical CPU.
- `Gen.Select`, `Select`, `SelectMany`, `Where`, `Gen.Const`, and range indexers such as `Gen.Int[min, max]` and `.Array[min, max]`, which include both ends.
