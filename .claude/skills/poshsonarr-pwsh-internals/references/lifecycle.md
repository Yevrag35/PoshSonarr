# Processing, stopping, and cleanup

Which of a cmdlet's methods run when its pipeline ends, stops early, or is stopped, and what a `PipelineStoppedException` does. Measured 2026-10-25 on PowerShell 7.6.8 with a throwaway `Add-Type` cmdlet that implements `IDisposable` and logs each method it runs and the thread it runs on, and another that throws `PipelineStoppedException` from `ProcessRecord`, each scenario in a new process.

## Which methods run

- **A pipeline that runs to its end:** `BeginProcessing`, `ProcessRecord`, `EndProcessing`, and then `Dispose`.
- **A downstream `Select-Object -First`** stops the cmdlet as soon as it has what it needs: the `WriteObject` call that hands it the last object it takes throws, and the cmdlet writes nothing more. `EndProcessing` and `StopProcessing` don't run, `Dispose` does, on the pipeline thread, and `$?` is `$true` after the statement.
- **A stopped pipeline:** `PowerShell.Stop()` calls `StopProcessing` on another thread while `ProcessRecord` is still running. `ProcessRecord` goes on until its next `WriteObject`, which throws `PipelineStoppedException`. `EndProcessing` doesn't run, and `Dispose` does, on the pipeline thread. `Stop()` returns, `EndInvoke` throws `PipelineStoppedException` ("The pipeline has been stopped."), the invocation state is `Stopped`, and the error stream is empty.
- **What `StopProcessing` throws is lost.** An exception, a `ThrowTerminatingError` with a record, and a `ThrowTerminatingError(null)` from it all leave the outcome as it was: `Stop()` returns, the pipeline ends the same way, and no error reaches the error stream, `EndInvoke`, or the invocation state.

## A command that throws `PipelineStoppedException`

- It stops the whole script, the way a stopped pipeline ends, and reports nothing. No error is shown, `$Error` stays empty, and `$?` is `$false` in a `finally` block. A `catch` block doesn't run, a `finally` block does, and no statement after it runs, neither in the function that called the command nor above it.
- `pwsh -File` then exits with 0, and `pwsh -Command` with 1.
- A hosting `PowerShell.Invoke()` returns without throwing, its invocation state is `Stopped`, with the exception as the reason, and its error stream is empty.
- The command upstream of it gets neither `EndProcessing` nor `StopProcessing`, only `Dispose`, on the pipeline thread.

## What it means for PoshSonarr

- To end a cmdlet because something failed, raise an error: `ThrowTerminatingError` to end the statement, or `WriteError` and return. Throw `PipelineStoppedException` only to stop everything, since the caller sees no error and a script run with `pwsh -File` exits with 0.
- Put cleanup that has to happen in `Dispose`. It runs on the pipeline thread after every ending measured: a normal end, a downstream `Select-Object -First`, a stop, and a `PipelineStoppedException` downstream. `EndProcessing` runs only after a normal end, and `StopProcessing` only after a stop.
- Keep `StopProcessing` to signaling, such as canceling the token of a request in flight. It runs on another thread while `ProcessRecord` can still be using the cmdlet's services, and it can't report an error: whatever it throws is lost.
