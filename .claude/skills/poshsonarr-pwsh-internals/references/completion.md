# Argument completion

## What a completer receives

Measured 2026-10-07 on PowerShell 7.6.6, with `TabExpansion2`, with completers on throwaway functions, and with a binary module's `IArgumentCompleter`, unless a fact gives another date.

- **The whole argument:** `wordToComplete` is the argument's whole text, wherever the cursor is in it, so `[guid]` with the cursor after `gu` arrives as `[guid]`. The results replace the whole argument too: their replacement range starts where the argument starts.
- **Strings:** a quoted argument arrives as its value between straight quotes. PowerShell unescapes the value, adds the closing quote when it's missing, and replaces typographic quotation marks, U+2018 through U+201E, with straight ones. So `'gu` arrives as `'gu'`, and so does `gu` after U+2018, with or without U+2019 after it. `gu` after U+201C arrives as `"gu"`, and `'it''s` as `'it's'`. A completer that matches the text as it arrives, with `*` after it, offers `Grabbed` for `Gra` and nothing for `'Gra` or `"Gra` (measured 2026-10-25 on PowerShell 7.6.8).
- **Commas:** PowerShell splits an argument that isn't in quotes at each comma, and passes only the part after the last comma: in `[System.Collections.Generic.Dictionary[string,in`, the completer gets `in`, and the results replace only `in`. In quotes, the whole string arrives.
- **Which parameter:** the completer runs for a named parameter, for its alias, for the `-Name:value` form, and for a positional argument. It runs for a cmdlet that has dynamic parameters too, with a dynamic switch before the argument.

## What a completer returns

Measured 2026-10-07 on PowerShell 7.6.6.

- An `IArgumentCompleter` that returns `null` gets PowerShell's default completion, which offers file names. One that returns an empty collection gets no completions at all.

## What PowerShell offers for `[ArgumentCompletions]`

Measured 2026-10-25 on PowerShell 7.6.8, with `TabExpansion2` on a throwaway binary cmdlet whose `[string]` parameter has `[ArgumentCompletions("SourceTitle", "Date", "Id", "EpisodeId", "two words", "Data")]`.

- It offers the values that start with the argument, in either case, in the attribute's order, not sorted: an empty argument gets all six in that order, and `D`, `d`, and `Da` get `Date` and `Data`.
- It doesn't quote a value that needs quotes: `t` gets `two words` as it is, which the command line then reads as two arguments.
- For a quoted argument, such as `'D` or `"D`, it offers nothing, and when no value matches, it offers nothing, not file names.
- It completes a positional argument, and after a comma it completes the part after the last comma, as in `Id, D`.
- It doesn't validate: a value that it doesn't list binds.

## What PowerShell offers for a ValidateSet

Measured 2026-10-24 on PowerShell 7.6.8, with `TabExpansion2` on a binary cmdlet's `[ValidateSet]` that ignores case.

- **Order:** completion offers the values sorted, not in the attribute's order, and puts a value that needs quotes, such as `!@$#%&`, in single quotes. It offered `'!@$#%&'` before `'!#%&'`.
- **Case:** an argument matches the values that start with it in either case, so `abc` and `ABCD` both get `abcdefghijklmnopqrstuvwxyz` and `ABCDEFGHIJKLMNOPQRSTUVWXYZ`. Two values that differ only in case are allowed: both bind, and completion offers both.

## What PowerShell offers for an enum parameter

Measured 2026-10-24 on PowerShell 7.6.8, with `TabExpansion2` on a throwaway `Add-Type` cmdlet whose parameter is a `[Flags]` enum.

- Completion offers every member's name, sorted, with `None` and members that combine others, such as `All`, among them.
- After a comma, as in `<name>,` and `<name>, D`, it completes the next name on its own, and it offers names that are already in the list again. Inside a quoted string, as in `'<name>, D`, it offers nothing.

## Parameter names after other parameters are bound

Measured 2026-10-24 on PowerShell 7.6.8, with `TabExpansion2` on throwaway `Add-Type` cmdlets that have two parameter sets, a default one and one with two mandatory parameters, and on one that adds a twin of each set, in which a mandatory parameter takes the place of another.

- **After a bare dash,** completion leaves out the parameters that no set still open to the call has: after one of the second set's mandatory parameters, it offers no parameter that only the default set has, and with the twin sets, it offers neither of two exclusive parameters after the other one.
- **After a prefix,** it leaves them out the same way unless none of the parameters that are left matches, and then it offers every parameter that matches: after one of the second set's parameters, `-P` gave `-ProgressAction` and `-PipelineVariable`, and `-Pa` gave the default set's parameter whose name starts with `Pa`.

## What it means for PoshSonarr

- A completer removes the quotes that PowerShell passes in `wordToComplete` before it matches, and puts quotes around a result that needs them, so a user who starts a quoted argument still gets completions.
- A completer returns an empty collection, never `null`, when it has nothing to offer, so that PowerShell doesn't offer file names instead.
- A completer can't see the part of an unquoted argument before a comma, so it completes each comma-separated value on its own.
- An `[ArgumentCompletions]` list shows in the attribute's order, so write it in the order users should see. A value with a space in it needs a completer that quotes it, and since any value binds, a `ValidateSet` or the cmdlet decides what's valid.
- The order of a `[ValidateSet]`'s values shows in `Get-Command` and in the list that a binding error gives, not in tab completion, so choose the order for those two.
- An enum parameter needs no argument completer, a `[Flags]` one included: PowerShell completes each name of a comma-separated list on its own.
- Parameter sets keep completion from offering a parameter that the bound ones exclude only after a bare dash. A prefix can still bring it back, so the binding error and the help have to make the exclusion clear on their own.
