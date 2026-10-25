# Binding input and parameters

## Models as input

Measured 2026-10-25 on PowerShell 7.6.8 with throwaway `Add-Type` models shaped like PoshSonarr's: `PSObject` subclasses that call `base(capacity)`, add their values as note properties, and insert a type name, one of them derived from another, and an interface that two unrelated ones implement. Throwaway cmdlets took one of the models, an array of it, the interface, or an array of the interface, from the pipeline and by name, behind a validation attribute that logged what it received.

- **The same instance binds.** A model from the pipeline, from a variable, by name, by position, or as the one output of a parenthesized command arrives as itself, to its own class, to a base class, and to an interface it implements, and the validation attribute receives that instance. `Where-Object`, `Sort-Object`, `Select-Object -First`, and `ForEach-Object { $_ }` pass the same instance on.
- **Arrays:** to an array parameter, each pipeline object arrives in a one-element array, and `ValidateElement` of a `ValidateEnumeratedArgumentsAttribute` receives each element. A named list, a variable that holds an array, a named `List[T]`, and a parenthesized command with several outputs arrive as one array. An array piped as one object, with `Write-Output -NoEnumerate`, binds as itself, while a piped `List[T]` is enumerated.
- **Input that doesn't fit:** a model of an unrelated class, a deserialized copy, a `Select-Object` projection, and a `[pscustomobject]` don't bind to an interface they don't implement. They don't bind to a model class either, unless the class has a public constructor that can build a model from them, as the next section shows. From the pipeline, each such object fails on its own with `InputObjectNotBound` ("The input object cannot be bound to any parameters for the command either because the command does not take pipeline input or the input and its properties do not match any of the parameters that take pipeline input."), and the objects before and after it still bind. By name, the whole argument fails with `CannotConvertArgumentNoMessage`, as in "Cannot convert the "@{Id=4; SeriesId=40}" value of type "Probe.EpisodeProbe" to type "Probe.SeriesProbe".", where the type of the value is the object's first type name.
- **`$null` and empty input:** `$null` to a mandatory model parameter, piped or named, reaches the validation attribute as `null`, and then fails with `ParameterArgumentValidationErrorNullNotAllowed` ("Cannot bind argument to parameter 'InputObject' because it is null."). To a mandatory array with a `ValidateEnumeratedArgumentsAttribute`, a `$null` argument fails in that attribute's base class before `ValidateElement` runs, with `ParameterArgumentValidationError` ("Cannot validate argument on parameter 'InputObject'. The argument is null, empty, or an element of the argument collection contains a null value. ..."), while a `$null` element of a named list reaches `ValidateElement` and then fails with `NullNotAllowed`. A named `@()` fails with `ParameterArgumentValidationErrorEmptyArrayNotAllowed`. A piped `@()` binds nothing, so `ProcessRecord` doesn't run, and `EndProcessing` does.
- **By property name:** a `ValueFromPipelineByPropertyName` parameter reads a model's note properties and converts them, so a note property `Id` that holds `'8'` binds an `[int]` parameter as 8. A model without the property fails with `InputObjectNotBound`.
- **`PSObject[]`:** a piped model arrives as itself. An `[int]` or a `[string]` arrives in a new `PSObject` that wraps it, from the pipeline and as an element of a named list.
- **Two sets:** with a default set whose `-Id <int[]>` is at position 0 and a second set whose only pipeline parameter is a model array, a piped model binds the second set, and a model given by position goes to `-Id`, where it fails with `CannotConvertArgumentNoMessage` (to `System.Int32`). With no arguments, the call fails with `MissingMandatoryParameter` under `-NonInteractive`.

## Models the binder builds

Measured 2026-10-25 on PowerShell 7.6.8 with the models above, with one more model whose only constructor is public and parameterless, which adds an `Id` note property and has a C# property `Number` with a public setter, and with the module's Debug build.

- **A public constructor with one parameter converts the argument.** `5`, piped to a cmdlet whose pipeline parameter is an array of a model with a public `(int)` constructor, bound a new model that the constructor built from 5, instead of failing with `InputObjectNotBound`.
- **A public parameterless constructor makes the model accept any hashtable and any custom object.** Piped or named, a hashtable, a `[pscustomobject]`, and a model of an unrelated class all bound to an array of that model. The binder built a new model and set each key or property on it as a note property: a key that matched the `Id` note property set it, a key named after the C# property `Number` added a note property `Number` and left the C# property at 0, and a key that matched nothing, such as `Bogus`, added a note property too, without an error. A cast, as in `[Model]@{ Id = 5 }`, converts the same way. A bare `5` didn't bind.
- **The module's models are such models.** `[MG.Sonarr.Next.Models.Series.SeriesObject]@{ Id = 5; Title = 'x' }` gets `Id` and `Title` note properties, while its C# `Id` stays 0 and its C# `Title` stays empty, and its only type names are `System.Management.Automation.PSCustomObject` and `System.Object`.
- **An interface never gets a new object:** a hashtable to an `ISeriesPipeable[]` parameter fails with `CannotConvertArgumentNoMessage`.

## Structs with implicit conversion operators

Measured 2026-10-25 on PowerShell 7.6.8 with throwaway structs shaped like the module's parameter types: one with implicit operators from `string` and `SecureString`, one with operators from `string` and `int`, and an `Either<T1, T2>` with operators from `T1` and `T2`, each bound by name, alone and in arrays.

- An argument of a type that has an operator goes through that operator: a string through the `string` one, a `SecureString` through the `SecureString` one, and an `[int]` through the `int` one, so `Either<string, int>` gets `5` as its `int` and `'5'` as its `string`.
- Any other value goes through the `string` operator, as its text, even where an `int` operator exists: `[long] 5` arrives as `'5'`, and `5.0` and `5.5` as `'5.0'` and `'5.5'`. So does an `[int]` when there's only a `string` operator.
- `$null` fails for a single struct with `ParameterBindingFailed` ("Cannot bind parameter 'Key' to the target. Exception setting "Key": "Cannot convert null to type ..."."), without calling the `string` operator, and binds `null` to an array of the struct. `''` goes through the `string` operator.
- In an array, each element takes its own operator, as in `5, 'abc'`.

## Enums

Measured 2026-10-24 on PowerShell 7.6.8 with a throwaway `Add-Type` cmdlet whose parameter is a `[Flags]` enum with a `None` member at 0, single-bit members, a member that combines them, and members whose names share a prefix, and on 2026-10-25 on PowerShell 7.6.8 with a cmdlet that takes a non-flags enum, `Auto` (0), `Manual` (1), `ManualMove` (2), and `Move` (3), and a `DayOfWeek[]`.

- **Names** match without regard to case. When the whole argument is one name, a unique prefix binds its member, and an exact name binds its member even when it's also the start of another member's name: `Manual` binds `Manual`, `ManualM` binds `ManualMove`, `Mo` binds `Move`, and `Man` fails with `CannotConvertArgumentNoMessage` ("... too similar or identical to the following enumerator names: Manual, ManualMove. Use a more specific identifier name."). Wildcards, such as `Au*`, match nothing, and a name that matches no member fails with a message that lists the members.
- **Lists:** the binder joins a list's elements with commas, parses the result, and ORs the members, for a non-flags enum as for a flags one, so one string with commas binds the same as the list, with spaces around the names ignored. Every name in a list has to be exact: `Au, Mo` fails, and a list fails as a whole when one of its names does. A non-flags list binds without an error whatever it ORs to: `Manual, ManualMove` binds `Move` (1 | 2), and `Auto, Move` binds `Move`. Integers combine too, a name and an integer combine, a string of digits binds as its number, and a repeated name counts once.
- **Integers:** an integer that a member defines binds that member, and one with a value or a bit that no member defines fails ("... due to enumeration values that are not valid ..."), as 9 does for the non-flags enum and 7 for `DayOfWeek`. The binder checks only the arguments that it converts: a value that already has the enum's type binds as it is, so `[enum]::ToObject($type, 64)` binds 64 for a flags enum whose members cover 31, from command output too.
- **Zero, empty, and `$null`:** `0` binds the member for 0, and with a member for 0, a 0 in a list adds nothing: `Manual, 0` binds `Manual`. For a flags enum with no member for 0, a lone `0` or `'0'` still binds 0, and so does a `-band` of two members that share no bits, which already has the enum's type, while a 0 in a list with another member fails. `''` and `@()` fail as a name that's too similar to every member, and `$null` fails with `ParameterBindingFailed` ("Cannot convert null to type ...").
- **Enum arrays:** each element converts on its own, so `Mon, Tue` binds `Monday` and `Tuesday`, while an ambiguous element, such as `T`, fails the whole argument. One string with commas is still one element, parsed as a list: `'Monday,Tuesday'` binds the single value `Wednesday` (1 | 2), and `'Mon,Tue'` fails. Repeated values stay, `@()` binds an empty array, and `$null` binds `null`.
- **A validation attribute** on a flags parameter receives every value that binds, converted or not, as the enum itself, unwrapped from the `PSObject` of command output, so it sees the 0 and the undefined bits that the binder lets through. Its error is `ParameterArgumentValidationError`: "Cannot validate argument on parameter '<name>'.", followed by its own message. An argument that the binder can't convert, such as 32 when the members cover 31, fails with the binder's error before the attribute runs.
- `Get-Command -Syntax` shows the enum's type name, as in `-Mode <ProbeMode>` and `-Day <DayOfWeek[]>`, not its members.

## `[object]` and `[object[]]` parameters

Measured 2026-10-03 on PowerShell 7.6.6 with a throwaway `Add-Type` cmdlet.

- **Pipeline to an `[object]` parameter:** every non-null object arrives wrapped in a `PSObject`, even an `[int]`. A piped array arrives as one `PSObject` around the `object[]`. `$null` arrives as `$null`.
- **Pipeline to an `[object[]]` parameter:** a scalar arrives in a one-element array, unwrapped from its `PSObject` unless it's a `[pscustomobject]`, and its instance notes are dropped. An array binds as itself, so it's flattened.
- **A named argument to an `[object[]]` parameter:** only an `IList`, after unwrapping, supplies its elements. A hashtable, a `HashSet[T]`, a `Queue`, a string, or a LINQ iterator is one element, and a single `PSObject`-wrapped argument keeps its wrapper.
- PowerShell never enumerates a string, whether in the pipeline, in `foreach`, or in its operators.

## Named arguments to `[object]`, `[string[]]`, and `[string]` parameters

Measured 2026-10-23 on PowerShell 7.6.8 with a throwaway `Add-Type` cmdlet.

- **To an `[object]` parameter:** a quoted string, or a variable that holds one, arrives as the raw `string`, and `5` as an `int`. A parenthesized command's output arrives wrapped: one object, as from `(Write-Output 'xy')` or a one-line `(Get-Content ...)`, is a `PSObject`, and several are an `object[]` of `PSObject`s. A comma list such as `'x', 'y'` is an `object[]` of the raw values, a typed array such as `[char[]]'xy'` or a `[string[]]` arrives as itself, `@()` is an empty `object[]`, and `$null` is `null`.
- **To a `[string[]]` parameter:** the binder converts each element, so `[char[]]'xy'` becomes `'x'` and `'y'`, one string per character. It unwraps `PSObject`-wrapped strings, turns `1, 2` into `'1'` and `'2'`, binds `''` as a one-element array that holds `''`, and binds `$null` as `null`.
- **To a `[string]` parameter:** `5` binds as `'5'`, and `$null` binds as `null`.

## What a transformation attribute receives

Measured 2026-10-23 on PowerShell 7.6.8 for named arguments, and on 2026-10-25 on PowerShell 7.6.8 for models and enum arrays, with throwaway `Add-Type` cmdlets whose `ArgumentTransformationAttribute` logged its input and returned it unchanged.

- It receives the argument before the binder converts it: a `char[]` as itself, one command output as a `PSObject`, several as an `object[]` of `PSObject`s, and a named list as an `object[]`, such as one of the strings in `-Day Mon, Tue` or of the models in `-InputObject $a, $b`. A single value arrives as itself, such as the string `'Monday'` or a model.
- For pipeline input, it receives each pipeline object as itself, not the one-element array that an array parameter then gets.
- On a parameter that isn't mandatory, PowerShell calls it for `$null` too.

## Enumerating a value the way the pipeline does

Measured 2026-10-25 on PowerShell 7.6.8, comparing what `ForEach-Object` received with what `LanguagePrimitives.GetEnumerator` and `GetEnumerable` gave and what `IsObjectEnumerable` said.

- `GetEnumerator` gives exactly the pipeline's elements for every value tried: arrays, a `List[T]`, a `HashSet[T]`, a `Queue[T]`, a `Stack[T]`, a LINQ iterator, a 2-D array (all its cells), a `BitArray`, a `NameValueCollection` (its keys), a `DataTable` (its rows), a `PSObject` around an array, and an enumerator such as `$hashtable.GetEnumerator()`.
- `GetEnumerable` matches too, except that it returns `null` for an `IEnumerator`, which the pipeline enumerates. `IsObjectEnumerable` is `$true` exactly where `GetEnumerable` returns a value.
- All three treat a string, a hashtable, an `XmlDocument`, a custom object, a scalar, and `$null` as one object, as the pipeline does: `GetEnumerator` and `GetEnumerable` return `null`, and `IsObjectEnumerable` is `$false`.
- None of them follows the rule for a named argument to an `[object[]]` parameter: all three enumerate sets, queues, and LINQ iterators.

## Dynamic parameters

Measured 2026-10-06 on PowerShell 7, with cmdlets that take a type as their first positional parameter, `-GenericType`, and whose `GetDynamicParameters` offers a `-CaseSensitive` switch only while that type is unbound or is one the switch applies to.

- With the positional argument first, as in `<cmdlet> [int] -CaseSensitive`, the call fails with `NamedParameterNotFound`. `GetDynamicParameters` sees `-GenericType` bound to `[int]` and offers no switch.
- With the switch first, as in `<cmdlet> -CaseSensitive [int]`, the call succeeds and the switch binds, although `GetDynamicParameters` offers no switch for `[int]`. While `-CaseSensitive` is still unknown, positional binding would take the argument after it as its value, so PowerShell calls `GetDynamicParameters` with `-GenericType` unbound, gets the switch, and binds `[int]` by position only afterward.
- A named `-GenericType ([int])` binds before PowerShell calls `GetDynamicParameters`, in either order.
- A dynamic parameter whose only parameter set excludes a bound static parameter fails with `ParameterNotInParameterSet` ("Parameter 'CaseSensitive' cannot be specified in parameter set 'WithComparingScript'."). That's a clearer error than the `NamedParameterNotFound` that withholding the parameter gives.

## Resolving parameter sets

Measured 2026-10-24 on PowerShell 7.6.8 with throwaway `Add-Type` cmdlets that have two parameter sets: the default one, with a parameter at position 0, and one with two mandatory parameters and nothing at position 0. A third parameter is at position 1 in every set. Two of the cmdlets add a twin of each set, in which a parameter that only the twins have takes the place of one that only the first two have: one cmdlet makes the twins' parameter mandatory there, and the other leaves it optional.

- **Mandatory in the twins:** every call without the twins' parameter resolves to the set that the first two sets alone give it, with the same values bound, splatted calls included. A call with the twins' parameter resolves to a twin, and one with both it and the parameter it replaces fails with `AmbiguousParameterSet`.
- **Optional in the twins:** a call with the two mandatory parameters and neither of the two exclusive parameters fails with `AmbiguousParameterSet`. Calls without the two mandatory parameters resolve as they do with the mandatory twins.
- **Positions:** a set with nothing at position 0 binds a lone positional argument to its parameter at position 1, so with the second set's two mandatory parameters named, a positional `12` binds the third parameter.
- **A missing mandatory parameter:** with the first two sets alone, a call that names one of the two mandatory parameters prompts for the other in an interactive session, and fails with `MissingMandatoryParameter` under `-NonInteractive`. With the twins, two sets are left, each missing a mandatory parameter, so the call fails with `AmbiguousParameterSet` without prompting.
- **The error:** `AmbiguousParameterSet`'s message is "Parameter set cannot be resolved using the specified named parameters. One or more parameters issued cannot be used together or an insufficient number of parameters were provided." It names no parameter.
- **`Get-Command -Syntax`** lists the default set first. The order of the others comes from the build, not from the source: the same declarations listed the twins in one order when PowerShell 7's `Add-Type` compiled them, and in another when Windows PowerShell 5.1's did. A twin's syntax shows its mandatory parameter without brackets.

## What it means for PoshSonarr

- A pipeline parameter typed as a pipeable interface, such as `ISeriesPipeable[]`, takes exactly the objects that implement it, so a hashtable, a projection, or a deserialized copy fails with `InputObjectNotBound`, one object at a time.
- A parameter typed as a model class takes more than that model when the class has a public parameterless constructor, as the module's models have: the binder turns any hashtable, any `[pscustomobject]`, and a model of another class into a new model that holds only note properties. Such a model's C# state, such as its C# `Id`, keeps its default, and it has none of the module's type names, so a check through the model's C# members sees those defaults, while one through its note properties sees the values the caller supplied.
- An ID validation attribute gets the model itself, or each element of an array, and passes an element whose ID is in range. It also gets `null`, from the pipeline and by name, before the binder's own null check, and each `null` element of a named list, so its null behavior decides what happens to those.
- A model given by position goes to the parameter at that position, such as an explicit-ID set's `-Id`, and fails to convert. Help, examples, and tests pass a model through the pipeline or by name.
- To `Either<string, int>`, only an `[int]` argument binds as a number. A `[long]`, a `[double]`, or a string of digits binds as text, through the `string` operator, so `[long] 607` reaches the cmdlet as the name `'607'`.
- A single-value enum parameter can't tell `Manual, ManualMove` from `Move` once it's bound, because the binder ORs a list even for a non-flags enum. When a list has to fail, a transformation attribute, which sees the raw `object[]`, refuses it.
- An enum-array parameter, such as `-DayOfWeek`, binds one quoted string with commas as a single OR'd value, so `'Monday,Tuesday'` is `Wednesday`. Help and examples write a list, as in `Monday, Tuesday`.
- Settle an enum's member names before it ships. A single name can be cut to a prefix of one member's name, so a member added later can make a script's prefix fit two, and the script fails. Exact names keep working, so help, tests, and examples use them.
- When 0 or an undefined bit means nothing to a cmdlet, refuse it in a validation attribute on the flags parameter, which sees every bound value and gives an error that names the parameter.
- A transformation attribute that works on lists, such as `DistinctValuesAttribute`, also gets a single value as itself, and `$null` on a parameter that isn't mandatory. Pipeline input reaches it one object at a time.
- A `PSObject[]` parameter gets a model as itself and any other value in a new `PSObject`, so code that tests an element's type unwraps it first.
- `GetDynamicParameters` alone can't validate a dynamic parameter against a positional argument. Check it again in `BeginProcessing`, against the final values.
- To make two parameters exclude each other through parameter sets, give each set a twin in which one parameter takes the other's place, and make it mandatory in the twins. Left optional there, it leaves the binder two sets for every call that names neither parameter, and every such call that the default set doesn't settle fails. Twin sets also turn a call that names one of two mandatory parameters from a prompt for the other into an `AmbiguousParameterSet` error, which names no parameter, so the help says which parameters exclude each other.
- Match parameter sets by name, in tests and anywhere else, and not by the order in which `Get-Command` lists them after the default set.
