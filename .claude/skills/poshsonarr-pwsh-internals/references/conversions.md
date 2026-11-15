# Converting values

## What `$null` converts to

`LanguagePrimitives.ConvertTo($null, T)`, measured 2026-10-25 on PowerShell 7.6.8:

| Type | Result |
|---|---|
| `[int]` | `0` |
| `[string]` | `''` |
| `[bool]` | `$false` |
| `[char]` | `[char]0` |
| `Nullable[int]`, arrays such as `[int[]]`, `[psobject]`, and most classes, such as `[version]`, `[System.IO.FileInfo]`, and `[hashtable]` | `$null` |
| `[ref]` | a `PSReference` that wraps `$null` |
| `[datetime]`, `[guid]`, `[timespan]`, enums such as `[ConsoleColor]` and `[DayOfWeek]`, and other structs such as `KeyValuePair[string, int]` | the conversion throws `PSInvalidCastException` |

## Enum names

Measured 2026-10-25 on PowerShell 7.6.8, with `LanguagePrimitives.ConvertTo` to `[DayOfWeek]`, to `[DayOfWeek[]]`, and to a non-flags enum whose members are `Auto`, `Manual`, `ManualMove`, and `Move`.

- A name matches without regard to case, and a unique prefix converts: `'Mon'` gives `Monday`, and `'Mo'` gives `Move`. An ambiguous prefix, such as `'T'` or `'Man'`, throws ("... too similar or identical to the following enumerator names ...").
- A string with commas ORs its names, for a non-flags enum too: `'Monday,Tuesday'` gives `Wednesday`, and `'Auto,Move'` gives `Move`.
- An integer, or a string of digits, that no member defines throws ("... due to enumeration values that are not valid ..."), as `7` and `'7'` do for `DayOfWeek`. A `[double]` with a whole value converts: `1.0` gives `Monday`.
- An array converts each element on its own: `@('Mon', 'Tue')` to `[DayOfWeek[]]` gives `Monday` and `Tuesday`.

## What a typed collection stores

Measured 2026-10-25 on PowerShell 7.6.8.

- A typed list converts what `$list.Add($x)` receives to its element type. In a `[System.Collections.Generic.List[int]]::new()`, `.Add('123')` stores `123` and `.Add($null)` stores `0`, and a `List[string]` stores `''` for `$null`.
- So `$null` stores what `ConvertTo` gives for the element type. For a struct whose conversion throws, `.Add($null)` fails: a `List[datetime]` throws `MethodException` ("Cannot find an overload for "Add" and the argument count: "1".").
- A typed dictionary converts its values the same way: `Add('b', $null)` and the indexer store `''` in a `Dictionary[string, string]`, `0` for `[int]` values, and `$null` for `[version]` values.

## What it means for PoshSonarr

- These facts cover `LanguagePrimitives.ConvertTo` and typed collections, not the parameter binder, which can differ: it refuses `$null` for a `[char]` parameter with `ParameterBindingFailed` instead of binding `[char]0` (measured 2026-10-23 on PowerShell 7.6.8). Measure how a parameter binds `$null` or a value of another type before you rely on this table for it.
- A transformation attribute that converts each element with `ConvertTo`, as `DistinctValuesAttribute` does, converts an enum name with a unique prefix and turns a string with commas into one OR'd value, as the binder does with each element of an enum array, so a list gives the same values with the attribute as without it.
