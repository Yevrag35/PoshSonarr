namespace MG.Sonarr.Next.Tests.Infrastructure;

/// <summary>
/// Stands in for a host whose pipeline thread runs posted work only when it's free, which it never is while a cmdlet
/// blocks on a task, as a GUI host's thread is. PowerShell's console host has no such context.
/// </summary>
/// <remarks>
/// The context counts what's posted or sent to it and runs none of it, so code that resumes on the caller's context
/// never resumes.
/// </remarks>
internal sealed class BlackHoleSynchronizationContext : SynchronizationContext
{
	private int _posts;

	/// <summary>
	/// Gets the number of callbacks that were posted or sent to the context.
	/// </summary>
	/// <value>The number of callbacks that the context swallowed.</value>
	internal int Posts => Volatile.Read(ref _posts);

	/// <inheritdoc/>
	public override SynchronizationContext CreateCopy()
	{
		return this;
	}

	/// <inheritdoc/>
	public override void Post(SendOrPostCallback d, object? state)
	{
		_ = Interlocked.Increment(ref _posts);
	}

	/// <inheritdoc/>
	public override void Send(SendOrPostCallback d, object? state)
	{
		_ = Interlocked.Increment(ref _posts);
	}

	/// <summary>
	/// Runs an action on a new thread whose current context is a new <see cref="BlackHoleSynchronizationContext"/>, and
	/// waits for the action to finish.
	/// </summary>
	/// <remarks>
	/// The thread is a background thread, so one whose action never finishes doesn't keep the test process alive.
	/// </remarks>
	/// <param name="action">The action to run, which blocks as a cmdlet does.</param>
	/// <param name="timeout">How long to wait for the action.</param>
	/// <returns>Whether the action finished in time, what it posted to the context, and what it threw.</returns>
	internal static BlackHoleRun Run(Action action, TimeSpan timeout)
	{
		BlackHoleSynchronizationContext context = new();
		Exception? error = null;

		Thread thread = new(() =>
		{
			SetSynchronizationContext(context);
			try
			{
				action();
			}
			catch (Exception e)
			{
				error = e;
			}
		})
		{
			IsBackground = true,
			Name = nameof(BlackHoleSynchronizationContext),
		};

		thread.Start();
		bool completed = thread.Join(timeout);

		return new(completed, context.Posts, completed ? error : null);
	}
}

/// <summary>
/// Holds the outcome of <see cref="BlackHoleSynchronizationContext.Run(Action, TimeSpan)"/>.
/// </summary>
/// <param name="Completed">Whether the action finished before the timeout.</param>
/// <param name="Posts">The number of callbacks that the action posted or sent to the context.</param>
/// <param name="Error">The exception that the action threw, or <see langword="null"/>.</param>
internal readonly record struct BlackHoleRun(bool Completed, int Posts, Exception? Error);
