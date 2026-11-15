# Reading a dump

The harness tags each method it prepares:

| Tag | Meaning |
|---|---|
| `direct` | `RuntimeHelpers.PrepareMethod` compiled it without running it: every non-virtual method, every override, and every virtual method on a struct or ref struct |
| `invoked` | A newslot virtual method on a class, compiled by calling it on an uninitialized instance with default arguments |
| `thunked` | The same, called through an emitted thunk, because a span-like return or parameter type rules out reflection |
| `virtual?` | A newslot virtual that the harness couldn't call, such as one declared on an abstract class. Its listing is in the dump only when `PrepareMethod` happened to compile it |
| `skip [needs --margs]` | A generic method that no `-MethodArgs` closed |

- A newslot virtual is one with a vtable slot of its own: every interface implementation, implicit or explicit, and every `virtual` member that isn't an override.
- `invoked` and `thunked` methods run until they throw, with null fields and default arguments. Compilation finishes before the first instruction runs, so the exception can't affect the listing, but a side effect that comes before the first null dereference, such as writing a file or setting a static field, does happen during the dump.
- A standalone listing is how the method compiles on its own. That's the point for a method that would otherwise inline into its caller and never be compiled by itself, the case this harness exists for. When a small method normally does inline, though, its caller's listing is the code that runs, so dump the caller when that difference matters. A helper the JIT never inlines, such as an always-throwing local function, compiles the same either way.
- An iterator method, and an async method in a build without runtime async, also gets its compiler-generated state machine's `MoveNext` prepared, and that listing is where its code is. In a Release build an async method's state machine is a struct, so its `MoveNext` is `direct` and doesn't run. Under runtime async an async method has no state machine; see Runtime async, below.

## Runtime async

With `<Features>runtime-async=on</Features>` in the build, the compiler gives an async method that returns `Task` or `Task<T>` the `Async` impl flag, 0x2000, instead of a state machine, and the JIT compiles the method's code as a resumable method. Iterators keep their state machines.

- The harness prepares such a method as `direct` and runs nothing, and the dump holds two listings under its name. The one whose header ends in the declared return type, such as ``Task`1[System.String]``, is a thunk that the runtime generates: it calls the other one, wraps a result that came back without suspending in `Task.FromResult`, creates an `AsyncHelpers+RuntimeAsyncTask` and calls `AsyncHelpers.FinalizeRuntimeAsyncTask` when the call suspended, and turns an exception into a task with `AsyncHelpers.TaskFromException`. The one whose header ends in the unwrapped type, such as `System.String`, and has an `; async` line, is the async variant, which holds the method's code (measured 2026-11-10 on .NET 11.0.0).
- An async variant takes a continuation as a hidden argument after `this`, so in `rcx` for a static method and in `rdx` for an instance method, and returns one in `rcx` beside its result in `rax`. A caller passes 0 and tests `rcx` after the call, and a nonzero `rcx` means the callee suspended: the caller saves its live values into a continuation of its own, from `CORINFO_HELP_ALLOC_CONTINUATION` or the one it was resumed with, and returns that. A method entered with a nonzero continuation is resuming, and jumps to a block that picks the resume point from the continuation's state and reloads the live values.
- An awaited call to a Task-returning method compiles to a call to the callee's async variant, which the listing names with the unwrapped return type, as in ``HttpContent:ReadAsStringAsync(System.Threading.CancellationToken):System.String:this``, or with none for `Task`. No task, awaiter, or `ConfigureAwait` struct appears on that path. A virtual call reaches the async variant through a vtable slot: in `DebugSerializeHandler.SendAsync`, the inlined `DelegatingHandler.SendAsync` calls `_innerHandler` with `call [rax+0x30]`, named with the unwrapped return type. A callee that isn't runtime-async itself, such as `HttpContent.ReadAsStringAsync`, which returns another method's task, gets an async variant from the runtime that wraps the Task-returning method, so the call still goes through the task that the method returns.
- The async variant saves the thread's `ExecutionContext` and `SynchronizationContext` on entry, through an inlined `AsyncHelpers.CaptureContexts`, and when it returns from a call that wasn't a resumption, it puts back whichever one changed, through an inlined `AsyncHelpers.RestoreContexts`, as `AsyncMethodBuilderCore.Start` does around a state machine's first `MoveNext`. A suspension at an await with `ConfigureAwait(false)` calls `AsyncHelpers.FinishSuspensionNoContinuationContext`.

## When the dump is empty

A 0-byte dump means `DOTNET_JitDisasm` matched nothing the harness compiled. In order of likelihood: the pattern is too narrow (a `-Pattern` needs its leading `*`), the only methods selected were tagged `virtual?` and `PrepareMethod` didn't compile them, or no method was selected. `-Summary` sets `DOTNET_JitDisasmSummary=1`, which adds one line per JIT-compiled method to the dump and tells a pattern that matched nothing apart from a method that never compiled. With `-Inlines` as well, the summary lines of methods that the trace's listener compiles on its own thread can land in the middle of a listing's lines, so read listings from a run without `-Summary` (measured 2026-11-10 on .NET 11.0.0).
