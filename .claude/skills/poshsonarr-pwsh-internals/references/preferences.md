# Preference variables and common parameters

What `$ErrorActionPreference`, `$DebugPreference`, and `$VerbosePreference` hold in each scope, what a binary cmdlet reads from them and from `MyInvocation.BoundParameters`, and how the engine applies them. Measured 2026-10-25 on PowerShell 7.6.8 with a throwaway `Add-Type` cmdlet that read each variable with `SessionState.PSVariable.GetValue` and `Get` and listed `MyInvocation.BoundParameters`, each scenario in a new process.

## What the variables hold

- **At the global scope,** each variable carries an `ArgumentTypeConverterAttribute` and holds an `ActionPreference`. Assigning `'Stop'` there stores `ActionPreference.Stop`.
- **In a function or a script,** `$ErrorActionPreference = 'Stop'` or `$DebugPreference = 'Continue'` creates a local variable without the attribute, which holds the `System.String` `'Stop'` or `'Continue'`, at the top of a script run with `&` too. A cmdlet that the function or the script calls reads that string, so a C# test such as `value is ActionPreference` is `false`. A typed assignment, `[System.Management.Automation.ActionPreference] $ErrorActionPreference = 'Stop'`, stores an `ActionPreference`.
- **The engine applies either.** Under a function's string `'Stop'`, a cmdlet's `WriteError` stops, and the error ends the whole script: neither the rest of the function nor the statement after its call runs, and `pwsh -Command` exits with 1.
- **Behind a script module,** a cmdlet that a module's function calls reads the module's scope chain, which ends at the global scope. It sees neither the calling function's local `'Stop'` nor its `'Continue'`, and its error doesn't stop there either, while a global `'Stop'` reaches it.

## Common parameters

- `-ErrorAction`, `-Debug`, and `-Verbose` don't change the variables that the cmdlet reads through `SessionState`. They're in `MyInvocation.BoundParameters`: `-ErrorAction Stop` and `-ErrorAction 1` as `ActionPreference.Stop`, `-Debug` and `-Verbose` as a `SwitchParameter` that's `True`, and `-Debug:$false` as one that's `False`.
- A value from `$PSDefaultParameterValues`, such as `'<cmdlet>:ErrorAction' = 'Stop'`, is in `BoundParameters` too, as an `ActionPreference`.

## What it means for PoshSonarr

- To follow the caller's preference when the common parameter isn't bound, as the base cmdlet's preference capture does, convert the variable's value to `ActionPreference` rather than test its type: a script or a function that sets the preference stores the string that its author wrote, and `LanguagePrimitives` converts it as it converts any enum name.
- A common parameter is in `BoundParameters` as an `ActionPreference` or a `SwitchParameter`, whether the caller typed it or `$PSDefaultParameterValues` supplied it.
- Reading a preference through `SessionState` sees the scope that the engine applies, behind a script module too, so once the value is converted, the cmdlet's own reading agrees with the engine.
