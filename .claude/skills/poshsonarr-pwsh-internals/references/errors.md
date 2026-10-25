# Errors

How a cmdlet's errors end a statement or the whole script, which exception types carry an error, and what `$Error`, `-ErrorVariable`, `-WarningVariable`, and `InvokeCommand.HasErrors` record.

## Ending a statement or the script

Measured 2026-10-03 on PowerShell 7.6.6, with `Add-Type` test cmdlets and script blocks that a cmdlet runs, unless a fact gives another date.

- `ThrowTerminatingError` ends only the statement. A cmdlet that catches an error from a script block it runs and calls `ThrowTerminatingError` makes even a `throw`, or a `Write-Error` under `Stop`, end only the statement, although either one ends the whole script when `ForEach-Object` runs it.
- A statement-ending error from a cmdlet ignores the cmdlet's own `-ErrorAction`: `-ErrorAction SilentlyContinue` doesn't hide it. Redirection doesn't either: with `2>$null` or `2>&1`, the host still reports it, and nothing of it reaches the output (measured 2026-10-25 on PowerShell 7.6.8, also with the module's `ContextNotSetException`).
- `throw 'boom'` gives the fully qualified error ID `boom`, with no cmdlet name.

## Exception types

Seen from C# that runs a script block (measured 2026-10-05 on PowerShell 7):

- `throw 'x'` is exactly a `RuntimeException`.
- `Write-Error` under `Stop` is an `ActionPreferenceStopException`. Its `ErrorRecord` is the record of the error that caused the stop, so its `ToString()` is that error's message, not "The running command stopped because..." (measured 2026-10-06).
- `break` is a `BreakException`, which is a `FlowControlException`, not a `RuntimeException`.
- `PSInvalidOperationException` derives from `InvalidOperationException`, not `RuntimeException`. Likewise, `PSInvalidCastException` derives from `InvalidCastException` (checked 2026-10-07). So C# overload resolution sends either one to an `Exception` overload, not a `RuntimeException` one.

## What `$Error` and the variable parameters hold

Measured 2026-10-25 on PowerShell 7.6.8 with a throwaway cmdlet that writes a sealed subclass of `ErrorRecord`, and another that reads `$Error` the way a cmdlet can, with `SessionState.PSVariable.GetValue("Error")`, which returns the `ArrayList` itself, unless a fact gives another date.

- **After `WriteError`,** under `Continue`, under `SilentlyContinue`, and with `2>$null`, `$Error[0]` is the very record that the cmdlet passed to `WriteError`, of the subclass, so `OfType<TRecord>()` finds it, and `-ErrorVariable` holds that record too. Under `Ignore`, `$Error` gets nothing.
- **After `Stop` or `ThrowTerminatingError`:** under `-ErrorAction Stop`, and after `ThrowTerminatingError`, `$Error[0]` is a new plain `ErrorRecord`, and so is the `$_` of a `catch` block, which is another one. Neither is the subclass, so `OfType<TRecord>()` finds nothing. Both keep the written record's exception, the same instance, and its fully qualified error ID and category. `$Error` gets one record, not two.
- `$Error` records the errors caught in a `try` block, hidden with `-ErrorAction SilentlyContinue`, or redirected with `2>$null`, but not the ones hidden with `-ErrorAction Ignore` (also measured 2026-10-06 on PowerShell 7). So `$Error` can't tell which errors a preference hid.
- `-WarningVariable` collects a cmdlet's warnings even under `-WarningAction SilentlyContinue` (measured 2026-10-06 on PowerShell 7).

## `InvokeCommand.HasErrors`

Measured 2026-10-25 on PowerShell 7.6.8 with throwaway cmdlets that write an error, and that read `InvokeCommand.HasErrors` in each processing method.

- It's `$false` until a command in the same pipeline writes an error, and `$true` from then on for every command in that pipeline: the writer, a command downstream of it, and a command upstream of it, in their later `ProcessRecord` calls and in `EndProcessing`.
- It counts an error whatever its `-ErrorAction`, `Ignore` included, and a pipeline object that failed to bind, with `InputObjectNotBound` or a validation error, counts too.
- It's per pipeline: the next statement starts at `$false`, in the same script block too. An error from `Write-Error` in a script block that `ForEach-Object` runs doesn't set it for the pipeline that `ForEach-Object` is in.

## Measuring traps

- Inside any `try` block, an error that ends a statement and one that ends the script both jump to `catch`, and Pester runs every test inside one. To tell them apart, run the command in a new process, outside any `try` block, with another statement after it. When that statement runs, the error ended only its own statement.

## What it means for PoshSonarr

- Choose between `ThrowTerminatingError` and `WriteError` by whether the rest of the call should still run. Neither `-ErrorAction` nor redirection can hide a statement-ending error, so don't raise one for a problem that a caller should be able to skip, such as one item that Sonarr doesn't have.
- Code that finds the last Sonarr error in `$Error`, as `Show-SonarrException` does, looks for its exception, a `SonarrHttpException`, on any record, not for a `SonarrErrorRecord`: after `-ErrorAction Stop`, `$ErrorActionPreference = 'Stop'`, or `ThrowTerminatingError`, `$Error` holds a plain `ErrorRecord` that keeps the same exception. After `-ErrorAction Ignore`, there's nothing to find.
- `InvokeCommand.HasErrors` tells whether anything in the pipeline has written an error, under any `-ErrorAction`. A cmdlet that has to react only to its own errors, or to its own input objects that failed to bind, keeps its own record of them.
- Don't use `$Error` to tell which errors a preference hid.
