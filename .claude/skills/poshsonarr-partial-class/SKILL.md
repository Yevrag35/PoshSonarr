---
name: poshsonarr-partial-class
description: When a request points you at a concern file of a split C# type in PoshSonarr, `TypeName-Concern.cs` (such as `SonarrHttpClient-Async.cs`, `Wildcard-Parsing.cs`, `SonarrCmdletBase-Write.cs`, or `RentedBuffer_T-State.cs`), also read the type's main file, `TypeName.cs` in the same folder, before you reason about or edit it, because the main file holds the type's doc comment, attributes, base list, generic constraints, fields, and constructors. Covers finding a concern file's main file and the type's other files, what a `_T` or `_2` suffix means and the non-generic type that shares the name without one, the item in `src/MG.Sonarr.Next/MG.Sonarr.Next.csproj` that nests concern files under their main file and the project entries that override it, how the compiler merges a split type's doc comments, an interface split beside its implementation, `file` types in concern files, and how to name and declare a new concern file. Use it whenever a request names a hyphenated `.cs` file or a partial type, and before you read or edit a concern file, add a member, field, constructor, interface member, or doc comment to a split type, split a type, or add, rename, or move one of its files. Skip it for a hyphenated file whose type isn't `partial`.
---

# Partial types in PoshSonarr

PoshSonarr splits a large C# type by concern into a main file, `TypeName.cs`, and concern files named `TypeName-Concern.cs` with the concern in PascalCase, all in one folder. Each file holds one `partial` declaration of the type. The main file declares the type in full, and a concern file declares it again with only its modifiers and type parameters, so a concern file read on its own hides the fields, constants, and interfaces that its members depend on.

- When you're pointed at a concern file, or you're about to read or edit one, read its main file too, even when the request names only the concern file.
- When a request names several concern files of one type, read the main file once.
- When you're handed the main file, the type's concern files hold the rest of its members. Read the ones the task touches.

## Finding the main file

- Take the concern file's name without `.cs`, keep the part before the last hyphen, and add `.cs`. The main file is in the same folder: `SonarrHttpClient-Async.cs` belongs to `SonarrHttpClient.cs`, and `SonarrCmdletBase-Write.cs` to `SonarrCmdletBase.cs`.
- A `_T` or `_2` suffix is part of the name and marks a generic type, `_T` for one type parameter named `T` and a number for that many type parameters. `RentedBuffer_T-State.cs` belongs to `RentedBuffer_T.cs`, which declares `RentedBuffer<T>`, and `Either_2.cs` declares `Either<T1, T2>`.
- The same name without the suffix holds a different type, a non-generic static class with factory methods for the generic one. `MetadataList.cs` declares `MetadataList`, and `MetadataList_T.cs` declares `MetadataList<T>`. Check that the main file declares the same type as the concern file, with the same type parameters.
- A project file can name a concern file's main file outright, with a `<Compile Update>` item for that one file that sets `<DependentUpon>`. That entry wins over the file's name, so look for one when the derived file doesn't exist or declares another type.
- The compiler joins a type's declarations by namespace and name, whatever their files are called. To find every file of a type, search its project for the type's `partial` declarations, such as with `partial struct Wildcard\b`, rather than going by file names or by a comment that lists the files.
- A type can have no main file, with every one of its files a concern file. Read the files the task touches, and search the others for the private members they share.

## What the main file holds

- The type's doc comment and attributes, such as `[StructLayout]` and `[DebuggerDisplay]`, its base class and interfaces, and a generic type's constraints. Because a concern file declares the type with only its modifiers and type parameters, as in `public readonly partial struct Wildcard` or `public sealed partial class MetadataList<T>`, the interfaces that its members implement appear only in the main file. `Wildcard-Parsing.cs` implements `ISpanParsable<Wildcard>`, for one, and only `Wildcard.cs` lists it.
- The type's fields and constructors, static ones included, such as the services that `SonarrHttpClient` takes in its constructor, along with the constants and static fields that the concern files share, such as `Wildcard`'s `DEFAULT_COMPARISON`. Look there for an existing field before you add one, and add new fields and constructors there.
- Other types that go with the main type, such as `SonarrClientDependencyInjection`, which registers `SonarrHttpClient`, in `SonarrHttpClient.cs`, and the `WildcardMatchType` enum in `Wildcard.cs`.

A `-State` concern file holds a nested `State` type, the type of the main file's `_state` field, rather than fields of the outer type, as `Wildcard-State.cs` and `RentedBuffer_T-State.cs` do.

## Doc comments

- The compiler merges the doc comments on all of a type's declarations into the type's one entry in the documentation file (measured 2026-10-25 with .NET SDK 10.0.402). Keep the type's `<summary>` and `<remarks>` in the main file, since a `<summary>` on a concern file's declaration gives the type a second one.
- A doc comment on a concern file's declaration adds to the type's docs. `SonarrCmdletBase-Processing.cs` uses one to add a `<provider>` element holding the `<param>` text for the `provider` parameter of `Begin`, `Process`, and `End`, which those methods pull in with `<inheritdoc cref="SonarrCmdletBase" path="/provider"/>`. When a project generates a documentation file, the compiler reports CS1572 for that `<param>`, because the type has no parameter named `provider`.

## Interfaces split with their implementation

A concern file can hold the part of an interface that its members implement. `SonarrHttpClient.cs` declares the synchronous members of `ISonarrClient` above the class, and `SonarrHttpClient-Async.cs` declares the asynchronous ones above their implementations. Add an interface member to the file that holds its implementation. The interface's doc comment stays in the main file, like the class's.

## File-local types

A `file` type is visible only in the file that declares it, so the type's other files can't use one that a concern file declares, such as `RentedBufferConstants` in `RentedBuffer_T-State.cs` or `ListMarshal` in `MetadataList_T-Marshalling.cs`. Code that needs one goes in that file.

## Nesting in Solution Explorer

`src/MG.Sonarr.Next/MG.Sonarr.Next.csproj` nests each concern file under its main file with this item:

```xml
<Compile Update="**\*-*.cs">
	<DependentUpon>$([System.Text.RegularExpressions.Regex]::Replace('$([System.String]::Copy(%(Filename)))', '^(.+)\-.+$', '$1.cs'))</DependentUpon>
</Compile>
```

- It matches every `.cs` file in the project with a hyphen in its name, and sets the file's `DependentUpon` to the part of its name before the last hyphen, plus `.cs`. The greedy `(.+)` is what makes it the last hyphen. Visual Studio looks for that file in the concern file's folder.
- On a `Compile` item, `DependentUpon` changes only how Solution Explorer shows the file. The compiler ignores it, so a file whose derived parent doesn't exist still builds.
- MSBuild applies `Update` items in the order the project file lists them, so a later `<Compile Update>` item for one file overrides the pattern's `DependentUpon`. Add one only for a file whose name can't follow the pattern.
- The other projects have no such item, so their concern files, such as `SonarrCmdletBase-Processing.cs` in `MG.Sonarr.Next.Shell.Bases`, sit beside their main files. Concern files are named the same way in every project.

## Adding a concern file

- Name it after the main file, a hyphen, and the concern in PascalCase, in the main file's folder, keeping a generic type's suffix, as in `MetadataList_T-Marshalling.cs`. Keep hyphens out of the concern, because the nesting item cuts at the last one.
- Declare the type in the main file's namespace with the same modifiers and type parameters, as in `internal sealed partial class SonarrHttpClient`, and leave the base list, constraints, attributes, and doc comment to the main file.
- Put the fields and constructors that the concern needs in the main file.
- When you split a type for the first time, add `partial` to the main file's declaration too. Every declaration of a partial type needs the modifier.
- In `MG.Sonarr.Next`, the nesting item picks up the new file by its name, so the project file needs no change.

## When this doesn't apply

A hyphen alone doesn't make a file a concern file. A file whose type isn't `partial` holds the whole type, and there's no main file to find, so check the declaration before you look for one.
