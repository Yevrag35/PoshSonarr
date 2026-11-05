using MG.Sonarr.Next.Collections;
using MG.Sonarr.Next.Exceptions;
using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Extensions.Pwsh;
using MG.Sonarr.Next.Models;
using System.Collections;

namespace MG.Sonarr.Next.Shell.Cmdlets.NonApi;

/// <summary>
/// Represents the <c>Show-SonarrException</c> cmdlet, which shows the errors that Sonarr returned in a failed
/// response.
/// </summary>
/// <remarks>
/// <para>
/// The cmdlet sends nothing to Sonarr and needs no connection. It takes a <see cref="SonarrHttpException"/> from the
/// pipeline or from <c>-InputObject</c>, or the <c>Exception</c> property of an error record piped to it, and writes
/// each error that Sonarr's response held, such as each validation failure of a <c>400</c>, as a copy with a
/// <c>RequestUri</c> property that names the request's URL.
/// </para>
/// <para>
/// When no exception is given, the cmdlet uses the exception of the most recent Sonarr error in <c>$Error</c>, and
/// writes a verbose message when there is none. An exception whose response held no errors writes nothing.
/// </para>
/// <para>
/// The cmdlet's alias is <c>Show-SonarrError</c>.
/// </para>
/// </remarks>
[Cmdlet(VerbsCommon.Show, "SonarrException")]
[Alias("Show-SonarrError")]
public sealed class ShowSonarrExceptionCmdlet : PSCmdlet
{
	const string ERROR = "error";
	bool _processedPipeline;

	/// <summary>
	/// Gets or sets the exception whose server errors the cmdlet writes.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The parameter takes pipeline input, both the exception itself and the <c>Exception</c> property of an object,
	/// such as an error record from <c>$Error</c> or <c>-ErrorVariable</c>.
	/// </para>
	/// <para>
	/// The parameter's alias is <c>-Exception</c>.
	/// </para>
	/// </remarks>
	/// <value>
	/// The exception, or <see langword="null"/> to use the one of the most recent Sonarr error in <c>$Error</c>.
	/// </value>
	[Parameter(Position = 0, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
	[Alias("Exception")]
	public SonarrHttpException InputObject { get; set; } = null!;

	/// <summary>
	/// Writes the server errors of the exception in <c>-InputObject</c>, when there is one.
	/// </summary>
	protected override void ProcessRecord()
	{
		if (this.InputObject is not null)
		{
			_processedPipeline = true;
			this.WriteDetails(this.InputObject);
		}
	}
	/// <summary>
	/// Writes the server errors of the most recent Sonarr error in <c>$Error</c>, when no exception was given.
	/// </summary>
	/// <remarks>
	/// When <c>$Error</c> holds no such error, the cmdlet writes a verbose message instead.
	/// </remarks>
	protected override void EndProcessing()
	{
		if (!_processedPipeline)
		{
			if (TryGetLastError(this.SessionState.PSVariable, out SonarrHttpException? exception))
			{
				this.WriteDetails(exception);
			}
			else
			{
				this.WriteVerbose("No readable exceptions have been recorded.");
			}
		}
	}

	/// <summary>
	/// Writes a copy of each server error that the specified exception holds, with a read-only <c>RequestUri</c>
	/// property added.
	/// </summary>
	/// <remarks>
	/// The copies leave the exception's own error objects unchanged. An exception that holds no errors writes nothing.
	/// </remarks>
	/// <param name="exception">The exception whose server errors are written.</param>
	private void WriteDetails(SonarrHttpException exception)
	{
		IErrorCollection errors = exception.ExtendedInfo;
		if (errors.Count <= 0)
		{
			return;
		}

		foreach (SonarrObject pso in errors)
		{
			PSObject copy = pso.Copy();
			copy.AddReadOnlyProperty(nameof(HttpRequestMessage.RequestUri), exception.RequestUri);

			this.WriteObject(copy);
		}
	}

	/// <summary>
	/// Finds the <see cref="SonarrHttpException"/> of the most recent Sonarr error in <c>$Error</c>.
	/// </summary>
	/// <remarks>
	/// <c>$Error</c> keeps its newest record first, so the method looks at the records from the newest on.
	/// </remarks>
	/// <param name="intrinsics">The session's variables, which hold <c>$Error</c>.</param>
	/// <param name="result">
	/// When the method returns, the exception that was found, or <see langword="null"/> when none was.
	/// </param>
	/// <returns><see langword="true"/> if an exception was found; otherwise, <see langword="false"/>.</returns>
	private static bool TryGetLastError(PSVariableIntrinsics intrinsics, [NotNullWhen(true)] out SonarrHttpException? result)
	{
		result = null;
		if (!intrinsics.TryGetVariableValue(ERROR, out ICollection? collection) || collection.Count <= 0)
		{
			return false;
		}

		IEnumerable<SonarrErrorRecord> records = collection.OfType<SonarrErrorRecord>();
		SonarrErrorRecord? record = records.FirstOrDefault();

		if (record is not null && record.Exception is SonarrHttpException httpEx)
		{
			result = httpEx;
			return true;
		}

		return false;
	}
}
