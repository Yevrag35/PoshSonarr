using MG.Sonarr.Next.Collections.Pools;
using MG.Sonarr.Next.Exceptions;
using MG.Sonarr.Next.Unions;

namespace MG.Sonarr.Next.Shell.Extensions;

public static partial class PSCmdletExtensions
{
    /// <summary>
    /// Writes the outcome of a command to the pipeline.
    /// </summary>
    /// <remarks>
    /// When the outcome is a success, the <typeparamref name="TSuccess"/> is written to the output stream; otherwise, 
    /// the <see cref="SonarrErrorRecord"/> is written to the error stream.
    /// </remarks>
    /// <typeparam name="TCmdlet">The type of PowerShell cmdlet this method is extending.</typeparam>
    /// <typeparam name="TSuccess">The type of object returned when the outcome is a success.</typeparam>
    /// <param name="cmdlet">The PowerShell cmdlet this method is extending</param>
    /// <param name="outcome">The outcome of the API call.</param>
    public static void WriteOutcome<TCmdlet, TSuccess>(this TCmdlet cmdlet, Either<TSuccess, SonarrErrorRecord> outcome) where TCmdlet : PSCmdlet
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
    /// When the outcome is a success, the <typeparamref name="TSuccess"/> is written to the output stream; otherwise, the
    /// <typeparamref name="TResetOnFail"/> state is reset and the <see cref="SonarrErrorRecord"/> is written to the
    /// error stream.
    /// </remarks>
    /// <typeparam name="TCmdlet">The type of PowerShell cmdlet this method is extending.</typeparam>
    /// <typeparam name="TSuccess">The type of object returned when the outcome is a success.</typeparam>
    /// <typeparam name="TResetOnFail">The state that will be reset if the outcome is a <see cref="SonarrErrorRecord"/>.</typeparam>
    /// <param name="cmdlet">The PowerShell cmdlet this method is extending</param>
    /// <param name="resettable">The stated object that will be reset if the outcome is a <see cref="SonarrErrorRecord"/>.</param>
    /// <param name="outcome">The outcome of the API call.</param>
    public static void WriteOutcome<TCmdlet, TSuccess, TResetOnFail>(this TCmdlet cmdlet,
        TResetOnFail resettable,
        Either<TSuccess, SonarrErrorRecord> outcome)
            where TCmdlet : PSCmdlet
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
    public static void WriteOutcome<TCmdlet, TSuccess>(this TCmdlet cmdlet,
        Either<TSuccess, SonarrErrorRecord> outcome,
        Action<TCmdlet, SonarrErrorRecord> onFailure)
            where TCmdlet : Cmdlet
    {
        WriteOutcome(cmdlet,
            outcome,
            onSuccess: static (cmdlet, success) => cmdlet.WriteObject(success),
            onFailure);
    }
    public static void WriteOutcome<TCmdlet, TSuccess, TState>(this TCmdlet cmdlet,
        TState state,
        Either<TSuccess, SonarrErrorRecord> outcome,
        Action<TCmdlet, TState, SonarrErrorRecord> onFailure)
            where TCmdlet : Cmdlet
    {
        WriteOutcome(cmdlet,
            state: state,
            outcome,
            onSuccess: static (cmdlet, _, success) => cmdlet.WriteObject(success),
            onFailure);
    }
    public static void WriteOutcome<TCmdlet, TSuccess, TFailure>(this TCmdlet cmdlet,
        Either<TSuccess, TFailure> outcome,
        Action<TCmdlet, TSuccess> onSuccess,
        Action<TCmdlet, TFailure> onFailure)
            where TCmdlet : Cmdlet
    {
        WriteOutcome(cmdlet, 
            state: (onSuccess, onFailure),
            outcome, 
            onSuccess: static (cmdlet, state, success) => state.onSuccess(cmdlet, success),
            onFailure: static (cmdlet, state, failure) => state.onFailure(cmdlet, failure));
    }
    public static void WriteOutcome<TCmdlet, TSuccess, TFailure, TState>(this TCmdlet cmdlet,
        TState state,
        Either<TSuccess, TFailure> outcome,
        Action<TCmdlet, TState, TSuccess> onSuccess,
        Action<TCmdlet, TState, TFailure> onFailure)
            where TCmdlet : Cmdlet
    {
        switch (outcome.Index)
        {
            case 1u:
                onSuccess(cmdlet, state, outcome.AsT1!);
                break;

            case 2u:
                onFailure(cmdlet, state, outcome.AsT2!);
                break;

            default:
                throw new EmptyStructException(nameof(outcome), typeof(Either<TSuccess, TFailure>), innerException: null);
        }
    }
}