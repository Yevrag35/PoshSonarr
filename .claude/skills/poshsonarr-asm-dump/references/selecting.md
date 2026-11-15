# Selecting methods to dump

What each invocation of the script selects, with `...` for the script's path:

| Invocation | Selects |
|---|---|
| `... Wildcard IsMatch` | Every overload of one method |
| `... WildcardSet` | Every declared method and constructor |
| `... SpanStringBuilder .ctor` | Constructors only |
| `... MetadataResolver Item` | An indexer, through the property fallback to `get_Item` and `set_Item` |
| `... WildcardSet 'System.Collections.IEnumerable.GetEnumerator'` | An explicit interface implementation, by its full member name |
| `... RentedBuffer AsSpan -TypeArgs char` | A method of a closed generic type |
| `... PooledBuffer+Enumerator MoveNext -TypeArgs int` | A nested type inside a generic one |
| `... TimedCmdlet -Assembly Next.Shell.Bases` | A type looked up in one assembly only, named in full or without its `MG.Sonarr.` prefix |

- A property name selects its `get_` and `set_` accessors. For an indexer renamed with `[IndexerName("X")]`, pass `X`. A method's overloads are always selected together.
- To find a member's exact name, pass a method name that doesn't exist. The harness then lists every method declared on the type, compiler-generated ones included.
- The type search covers the `MG.Sonarr*` assemblies that the harness reaches through its reference to `MG.Sonarr.Next.Shell`, which references the module's other projects. A type in a project outside that closure needs a `ProjectReference` in `src/MG.Sonarr.Tools.Asm/MG.Sonarr.Tools.Asm.csproj` first.
- The default pattern, `*<Class>*:*`, covers the whole class on purpose: it also catches state machines, local functions, and lambda display classes that a `*Class:Method` pattern would miss. It holds only what the harness compiles because, with tiering off, little else is JIT-compiled at all (measured 2026-10-25 on .NET 10.0.13). A `-Pattern` of your own needs a leading `*`, because the JIT matches the namespace-qualified class name.

## Generic types and methods

- `-TypeArgs` closes the declaring type, and `-MethodArgs` (alias `-MethodTypeArgs`) closes a generic method's own type parameters. Both take aliases such as `int` and `string`, BCL full names, and MG.Sonarr type names.
- A missing `-TypeArgs` ends the run with exit code 2 and the type's arity. A generic method without `-MethodArgs` is skipped on its own, as `skip [needs --margs]`, and a wrong count shows up as that method's `MakeGenericMethod` error.
- When two types share a simple name, the `-TypeArgs` count picks one: `RentedBuffer` with `-TypeArgs char` is `RentedBuffer<T>`, and without it the static `RentedBuffer` factory class.
- A reference-type argument compiles to shared code, listed with `System.__Canon`, as in ``RentedBuffer`1[System.__Canon]:AsSpan()``. That shared code is what runs for every reference type, so it's the right listing, not a degraded one.
- A static shared generic method takes its instantiation as a hidden first argument, in `rcx`, so its declared parameters start one register later, in `rdx`. An instance method of a shared generic struct takes it after `this`, in `rdx`, so its declared parameters start in `r8`: `Either<T1, T2>.TryGetT1` for `__Canon` writes its two `out` parameters through `r8` and `r9` (measured 2026-11-15 on .NET 11.0.0).
- In shared code, a call whose target depends on a type argument names no target in the listing: in `CollectionExtensions.AddMany` with `-MethodArgs string`, the interface call to `ICollection<T>.Add` is a bare `call [r11]`, with `r11` copied from a slot of the method's generic dictionary. The dictionary lookup sits before the loop, and its `CORINFO_HELP_RUNTIMEHANDLE_METHOD` call, which fills a slot that's still empty, sits after the `ret`. With `-MethodArgs int`, the listing names the same call, ``call [r11]System.Collections.Generic.ICollection`1[int]:Add(int):this``, and loads `r11` as a constant (measured 2026-11-03 on .NET 10.0.14). To find out which call a bare `call [r11]` is, dump a value-type instantiation.
- The C# nullable shorthand works: `-TypeArgs 'int?'` or `'WildcardMatchType?'` closes over `Nullable<T>`, and `'string?'` is `string`, as it is at runtime. The raw reflection form, ``'System.Nullable`1[System.Int32]'``, needs a BCL full name inside the brackets and can't name an MG.Sonarr type there, because assembly-qualifying one takes a comma, and a comma separates `-TypeArgs` values.
- A type argument that breaks a constraint reports `violates the constraint of type 'T'`, as `MetadataList` does with `int`, while one that doesn't resolve reports `type argument '...' not found`. The first means the name resolved and the type is wrong for that slot.
- `-Assembly` narrows the type-argument lookup too: an MG.Sonarr type name in `-TypeArgs` or `-MethodArgs` resolves only from the assembly that `-Assembly` names, and one from another assembly reports `type argument '...' not found`, while aliases such as `string` and BCL names resolve either way (measured 2026-11-11 on .NET 11.0.0). Leave `-Assembly` off when the type's simple name is unique, or, since every reference-type argument compiles to the same `__Canon` code, name any class from that assembly, or `string`, that meets the constraint.

## Nested types

- Name a nested type as `Outer+Inner`. When the outer type is generic, the nested type shares its type parameters, so it needs `-TypeArgs` too, as in `PooledBuffer+Enumerator MoveNext -TypeArgs int`. Leaving `-TypeArgs` out ends the run with exit code 2.
- A nested type's simple name alone, such as `Enumerator`, matches every type of that name, and the harness lists them and stops.

## Local functions and lambdas

- The compiler emits a local function as a method of its declaring type, so a class-wide run prepares it with no extra arguments. To target one, pass its exact compiled name, quoted for PowerShell's angle brackets and pipe, as in `SpanStringBuilder '<Grow>g__throwTooBig|73_0'`.
- A local function declared inside a generic method has that method's type parameters, so it needs `-MethodArgs`, as `rentFromLength` inside `RentedBuffer.Rent<T>` does: `RentedBuffer '<Rent>g__rentFromLength|5_0' -MethodArgs char`.
- The numbers at the end of a compiled name follow the method's position in its type, and they change when members above it are added or moved. A saved command line goes stale that way, and so does the dump name it derives. Pass a stable `-OutFile` for any comparison across edits, and for runs of one generic type with different type arguments, which would otherwise share a dump name and rotate over each other's baseline.
- A lambda compiles into a method of a nested display class, `<>c` or `<>c__DisplayClass*`. Target the class through its outer type, as in `'ObjectConverter+<>c'`. The bare `'<>c'` is ambiguous, and its pattern also matches the harness's own lambdas, which compile during every run.
