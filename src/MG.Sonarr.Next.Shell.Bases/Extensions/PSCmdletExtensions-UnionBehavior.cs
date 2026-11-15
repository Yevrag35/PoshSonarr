using MG.Sonarr.Next.Exceptions;
using MG.Sonarr.Next.Models;
using MG.Sonarr.Next.Unions;

namespace MG.Sonarr.Next.Shell.Extensions;

public static partial class PSCmdletExtensions
{
	/// <summary>
	/// Writes the outcome of a command to the pipeline.
	/// </summary>
	/// <remarks>
	/// When the outcome is a success, the <typeparamref name="TSuccess"/> is written to the output stream; otherwise,
	/// the <see cref="SonarrErrorRecord"/> is written to the error stream, unless its
	/// <see cref="SonarrErrorRecord.IsIgnorable"/> is <see langword="true"/>, as it is for a 404, in which case nothing
	/// is written.
	/// </remarks>
	/// <typeparam name="TCmdlet">The type of PowerShell cmdlet this method is extending.</typeparam>
	/// <typeparam name="TSuccess">The type of object returned when the outcome is a success.</typeparam>
	/// <param name="cmdlet">The PowerShell cmdlet this method is extending.</param>
	/// <param name="outcome">The outcome of the API call.</param>
	/// <exception cref="EmptyStructException">Thrown when <paramref name="outcome"/> is the default value, which holds
	/// neither a success nor an error.</exception>
	public static void WriteOutcome<TCmdlet, TSuccess>(this TCmdlet cmdlet, ObjEither<TSuccess, SonarrErrorRecord> outcome)
		where TCmdlet : PSCmdlet
		where TSuccess : class
	{
		WriteOutcome(cmdlet,
			outcome,
			onFailure: static (cmdlet, failure) =>
			{
				if (!failure.IsIgnorable)
				{
					cmdlet.WriteError(failure);
				}
			});
	}
	/// <summary>
	/// Writes the outcome of a command to the pipeline.
	/// </summary>
	/// <remarks>
	/// When the outcome is a success, the <typeparamref name="TSuccess"/> is written to the output stream, and
	/// <paramref name="resettable"/> is left as it is, for the caller to commit. Otherwise, <paramref name="resettable"/>
	/// is reset through <see cref="IResettable.TryReset"/>, and the <see cref="SonarrErrorRecord"/> is written to the
	/// error stream, unless its <see cref="SonarrErrorRecord.IsIgnorable"/> is <see langword="true"/>, in which case
	/// nothing is written. A reset that returns <see langword="false"/> fails an assertion in Debug builds and is
	/// otherwise ignored.
	/// </remarks>
	/// <typeparam name="TCmdlet">The type of PowerShell cmdlet this method is extending.</typeparam>
	/// <typeparam name="TSuccess">The type of object returned when the outcome is a success.</typeparam>
	/// <typeparam name="TResetOnFail">The type of the object that is reset if the outcome is a
	/// <see cref="SonarrErrorRecord"/>.</typeparam>
	/// <param name="cmdlet">The PowerShell cmdlet this method is extending.</param>
	/// <param name="resettable">The stateful object that is reset if the outcome is a
	/// <see cref="SonarrErrorRecord"/>, such as the model that the request sent.</param>
	/// <param name="outcome">The outcome of the API call.</param>
	/// <exception cref="EmptyStructException">Thrown when <paramref name="outcome"/> is the default value, which holds
	/// neither a success nor an error.</exception>
	public static void WriteOutcome<TCmdlet, TSuccess, TResetOnFail>(this TCmdlet cmdlet,
		TResetOnFail resettable,
		ObjEither<TSuccess, SonarrErrorRecord> outcome)
			where TCmdlet : PSCmdlet
			where TSuccess : class
			where TResetOnFail : IResettable
	{
		WriteOutcome(cmdlet,
			state: resettable,
			outcome,
			onFailure: static (cmdlet, state, failure) =>
			{
				bool reset = state.TryReset();
				Debug.Assert(reset, "Failed to reset state.");
				if (!failure.IsIgnorable)
				{
					cmdlet.WriteError(failure);
				}
			});
	}
	/// <summary>
	/// Writes a successful outcome to the output stream, or passes a failed one to the specified callback.
	/// </summary>
	/// <remarks>
	/// The callback decides what happens to the error, and the method writes nothing for it, an ignorable one included.
	/// </remarks>
	/// <typeparam name="TCmdlet">The type of PowerShell cmdlet this method is extending.</typeparam>
	/// <typeparam name="TSuccess">The type of object returned when the outcome is a success.</typeparam>
	/// <param name="cmdlet">The PowerShell cmdlet this method is extending.</param>
	/// <param name="outcome">The outcome of the API call.</param>
	/// <param name="onFailure">The callback that receives <paramref name="cmdlet"/> and the error when the outcome is a
	/// failure.</param>
	/// <exception cref="EmptyStructException">Thrown when <paramref name="outcome"/> is the default value, which holds
	/// neither a success nor an error.</exception>
	public static void WriteOutcome<TCmdlet, TSuccess>(this TCmdlet cmdlet,
		ObjEither<TSuccess, SonarrErrorRecord> outcome,
		Action<TCmdlet, SonarrErrorRecord> onFailure)
			where TCmdlet : Cmdlet
			where TSuccess : class
	{
		WriteOutcome(cmdlet,
			outcome,
			onSuccess: static (cmdlet, success) => cmdlet.WriteObject(success),
			onFailure);
	}
	/// <summary>
	/// Writes a successful outcome to the output stream, or passes a failed one, with the specified state, to the
	/// specified callback.
	/// </summary>
	/// <remarks>
	/// The callback decides what happens to the error, and the method writes nothing for it, an ignorable one included.
	/// Passing state lets the callback be a static lambda that captures nothing.
	/// </remarks>
	/// <typeparam name="TCmdlet">The type of PowerShell cmdlet this method is extending.</typeparam>
	/// <typeparam name="TSuccess">The type of object returned when the outcome is a success.</typeparam>
	/// <typeparam name="TState">The type of the state that the callback receives.</typeparam>
	/// <param name="cmdlet">The PowerShell cmdlet this method is extending.</param>
	/// <param name="state">The state to pass to <paramref name="onFailure"/>.</param>
	/// <param name="outcome">The outcome of the API call.</param>
	/// <param name="onFailure">The callback that receives <paramref name="cmdlet"/>, <paramref name="state"/>, and the
	/// error when the outcome is a failure.</param>
	/// <exception cref="EmptyStructException">Thrown when <paramref name="outcome"/> is the default value, which holds
	/// neither a success nor an error.</exception>
	public static void WriteOutcome<TCmdlet, TSuccess, TState>(this TCmdlet cmdlet,
		TState state,
		ObjEither<TSuccess, SonarrErrorRecord> outcome,
		Action<TCmdlet, TState, SonarrErrorRecord> onFailure)
			where TCmdlet : Cmdlet
			where TSuccess : class
	{
		WriteOutcome(cmdlet,
			state: state,
			outcome,
			onSuccess: static (cmdlet, _, success) => cmdlet.WriteObject(success),
			onFailure);
	}
	/// <summary>
	/// Passes an outcome to the success callback or the failure callback, whichever it holds.
	/// </summary>
	/// <remarks>
	/// The method writes nothing itself, so the callbacks decide what reaches the pipeline.
	/// </remarks>
	/// <typeparam name="TCmdlet">The type of PowerShell cmdlet this method is extending.</typeparam>
	/// <typeparam name="TSuccess">The type of the outcome's success value.</typeparam>
	/// <typeparam name="TFailure">The type of the outcome's failure value.</typeparam>
	/// <param name="cmdlet">The PowerShell cmdlet this method is extending.</param>
	/// <param name="outcome">The outcome to act on.</param>
	/// <param name="onSuccess">The callback that receives <paramref name="cmdlet"/> and the success value.</param>
	/// <param name="onFailure">The callback that receives <paramref name="cmdlet"/> and the failure value.</param>
	/// <exception cref="EmptyStructException">Thrown when <paramref name="outcome"/> is the default value, which holds
	/// neither a success nor a failure.</exception>
	public static void WriteOutcome<TCmdlet, TSuccess, TFailure>(this TCmdlet cmdlet,
		ObjEither<TSuccess, TFailure> outcome,
		Action<TCmdlet, TSuccess> onSuccess,
		Action<TCmdlet, TFailure> onFailure)
			where TCmdlet : Cmdlet
			where TSuccess : class
			where TFailure : class
	{
		WriteOutcome(cmdlet,
			state: (onSuccess, onFailure),
			outcome,
			onSuccess: static (cmdlet, state, success) => state.onSuccess(cmdlet, success),
			onFailure: static (cmdlet, state, failure) => state.onFailure(cmdlet, failure));
	}
	/// <summary>
	/// Passes an outcome, with the specified state, to the success callback or the failure callback, whichever it holds.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The method writes nothing itself, so the callbacks decide what reaches the pipeline. Every other
	/// <c>WriteOutcome</c> overload ends here.
	/// </para>
	/// <para>
	/// Passing state lets the callbacks be static lambdas that capture nothing.
	/// </para>
	/// </remarks>
	/// <typeparam name="TCmdlet">The type of PowerShell cmdlet this method is extending.</typeparam>
	/// <typeparam name="TSuccess">The type of the outcome's success value.</typeparam>
	/// <typeparam name="TFailure">The type of the outcome's failure value.</typeparam>
	/// <typeparam name="TState">The type of the state that the callbacks receive.</typeparam>
	/// <param name="cmdlet">The PowerShell cmdlet this method is extending.</param>
	/// <param name="state">The state to pass to whichever callback runs.</param>
	/// <param name="outcome">The outcome to act on.</param>
	/// <param name="onSuccess">The callback that receives <paramref name="cmdlet"/>, <paramref name="state"/>, and the
	/// success value.</param>
	/// <param name="onFailure">The callback that receives <paramref name="cmdlet"/>, <paramref name="state"/>, and the
	/// failure value.</param>
	/// <exception cref="EmptyStructException">Thrown when <paramref name="outcome"/> is the default value, which holds
	/// neither a success nor a failure.</exception>
	public static void WriteOutcome<TCmdlet, TSuccess, TFailure, TState>(this TCmdlet cmdlet,
		TState state,
		ObjEither<TSuccess, TFailure> outcome,
		Action<TCmdlet, TState, TSuccess> onSuccess,
		Action<TCmdlet, TState, TFailure> onFailure)
			where TCmdlet : Cmdlet
			where TSuccess : class
			where TFailure : class
	{
		if (outcome.IsNeither)
		{
			throw new EmptyStructException(nameof(outcome), typeof(ObjEither<TSuccess, TFailure>), innerException: null);
		}

		// TryGetT1 reads the value by its index, where AsT1 and AsT2 would test its type.
		if (outcome.TryGetT1(out TSuccess? success, out TFailure? failure))
		{
			onSuccess(cmdlet, state, success);
		}
		else
		{
			onFailure(cmdlet, state, failure);
		}
	}
}