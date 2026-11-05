using MG.Sonarr.Next.Exceptions;
using MG.Sonarr.Next.Exceptions.Shell;
using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Services.Http;
using MG.Sonarr.Next.Strings;
using MG.Sonarr.Resources;
using System.Net;
using System.Text.Json;

namespace MG.Sonarr.Next.Shell.Cmdlets.Bases;

/// <summary>
/// Represents a <see cref="SonarrCmdletBase"/> that implements <see cref="IApiCmdlet"/> and times the Sonarr API
/// requests that it sends.
/// </summary>
/// <remarks>
/// <para>
/// The cmdlet captures its verbose and debug preferences when processing begins, before
/// <see cref="SonarrCmdletBase.Begin(IServiceProvider)"/> runs, and the HTTP handlers read them through
/// <see cref="CanWriteVerbose"/>, <see cref="CanDebugSerializeBefore"/>, and <see cref="CanDebugSerializeAfter"/> to
/// decide what to write for each request that the cmdlet sends.
/// </para>
/// <para>
/// The handlers call the cmdlet's write methods after an <see langword="await"/>, often on a thread-pool thread rather
/// than the pipeline thread, so the cmdlet writes its lines through the host's UI rather than through
/// <see cref="Cmdlet.WriteVerbose(string)"/> and <see cref="Cmdlet.WriteDebug(string)"/>, which throw off the pipeline
/// thread. Those lines go straight to the host, so redirecting the verbose or debug stream doesn't capture them and a
/// transcript doesn't record them.
/// </para>
/// <para>
/// The verbose line after a response reports the time since the cmdlet last called <see cref="StartTimer"/>. The
/// <c>Send*Request</c> helpers of <see cref="SonarrApiCmdletBase"/> call it, and a cmdlet that sends a request through a
/// client or a service itself calls it before each request.
/// </para>
/// </remarks>
//[DebuggerStepThrough]
public abstract class TimedCmdlet : SonarrCmdletBase, IApiCmdlet
{
	private long _timestamp;

	/// <summary>
	/// Gets a value that indicates whether the cmdlet captures its debug preference before processing begins.
	/// </summary>
	/// <remarks>
	/// This class returns <see langword="true"/>, so that <see cref="CanDebugSerializeBefore"/> and
	/// <see cref="CanDebugSerializeAfter"/> read the preference that <c>-Debug</c> or <c>$DebugPreference</c> gives.
	/// </remarks>
	/// <value>
	/// <see langword="true"/> if the cmdlet captures its debug preference; otherwise, <see langword="false"/>.
	/// </value>
	protected override bool CaptureDebugPreference => true;
	/// <summary>
	/// Gets a value that indicates whether the cmdlet captures its verbose preference before processing begins.
	/// </summary>
	/// <remarks>
	/// This class always returns <see langword="true"/> and seals the property, because <see cref="CanWriteVerbose"/>
	/// reads the preference that <c>-Verbose</c> or <c>$VerbosePreference</c> gives.
	/// </remarks>
	/// <value>Always <see langword="true"/>.</value>
	protected sealed override bool CaptureVerbosePreference => true;
	/// <summary>
	/// Gets a value that indicates whether the request's JSON body is written to the debug stream before the request is
	/// sent.
	/// </summary>
	/// <remarks>
	/// A cmdlet that writes its request body to the debug stream itself overrides this property to return
	/// <see langword="false"/>, so that the body appears once.
	/// </remarks>
	/// <value>
	/// <see langword="true"/> if <see cref="SonarrCmdletBase.DebugPreference"/> is anything other than
	/// <see cref="ActionPreference.SilentlyContinue"/>; otherwise, <see langword="false"/>.
	/// </value>
	public virtual bool CanDebugSerializeBefore => this.DebugPreference != ActionPreference.SilentlyContinue;
	/// <summary>
	/// Gets a value that indicates whether the response's JSON body is written to the debug stream after the response is
	/// received.
	/// </summary>
	/// <value>
	/// <see langword="true"/> if <see cref="SonarrCmdletBase.DebugPreference"/> is anything other than
	/// <see cref="ActionPreference.SilentlyContinue"/>; otherwise, <see langword="false"/>.
	/// </value>
	public virtual bool CanDebugSerializeAfter => this.DebugPreference != ActionPreference.SilentlyContinue;
	/// <summary>
	/// Gets a value that indicates whether the cmdlet writes verbose messages about its requests.
	/// </summary>
	/// <remarks>
	/// The preference is captured when processing begins, after
	/// <see cref="SonarrCmdletBase.OnCreatingScope(IServiceProvider)"/> runs, so a request sent before then writes no
	/// verbose messages.
	/// </remarks>
	/// <value>
	/// <see langword="true"/> if <see cref="SonarrCmdletBase.VerbosePreference"/> is neither
	/// <see cref="ActionPreference.SilentlyContinue"/> nor <see cref="ActionPreference.Ignore"/>; otherwise,
	/// <see langword="false"/>.
	/// </value>
	public bool CanWriteVerbose => !(this.VerbosePreference is ActionPreference.SilentlyContinue or ActionPreference.Ignore);

	/// <summary>
	/// Starts the timer that the verbose line after a response reports.
	/// </summary>
	/// <remarks>
	/// Each call restarts the timer, so the next response's line reports the time since the latest call.
	/// </remarks>
	protected void StartTimer()
	{
		_timestamp = Stopwatch.GetTimestamp();
	}

	/// <summary>
	/// Returns the time that has passed since <see cref="StartTimer"/> last ran.
	/// </summary>
	/// <remarks>
	/// The timer keeps running, so a second call returns a longer time. A call before <see cref="StartTimer"/> has run
	/// returns a time that measures nothing.
	/// </remarks>
	/// <returns>The time since the timer last started.</returns>
	protected TimeSpan StopTimer()
	{
		return Stopwatch.GetElapsedTime(_timestamp);
	}

	/// <summary>
	/// Writes a verbose line that reports the response to a request, with the time since the timer started and the
	/// response's status code.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The line reads like <c>Received response after 25ms - 404 (Not Found)</c>, and it goes to the host's UI, so it
	/// can be written from any thread. When the host has no UI, nothing is written. The method doesn't check
	/// <see cref="CanWriteVerbose"/>; the caller does.
	/// </para>
	/// <para>
	/// <paramref name="provider"/> and <paramref name="options"/> aren't used.
	/// </para>
	/// </remarks>
	/// <param name="response">The outcome of the request, with the response's status code and URL.</param>
	/// <param name="provider">A scoped service provider that the cmdlet can resolve services from.</param>
	/// <param name="options">
	/// The JSON options that deserialize the response, or <see langword="null"/> when there are none.
	/// </param>
	public void WriteVerboseAfter(ISonarrResponse response, IServiceProvider provider, JsonSerializerOptions? options)
	{
		if (this.Host?.UI is { } ui)
		{
			TimeSpan elapsed = this.StopTimer();
			string msg = GetAfterMessage(elapsed, response.StatusCode);
			ui.WriteVerboseLine(msg);
		}
	}

	/// <summary>
	/// Builds the verbose line that reports a response, with the time that the request took and the response's status.
	/// </summary>
	/// <param name="elapsedTime">The time since the timer started.</param>
	/// <param name="statusCode">The status code of the response.</param>
	/// <returns>The line, such as <c>Received response after 25ms - 404 (Not Found)</c>.</returns>
	private static string GetAfterMessage(TimedValue elapsedTime, HttpStatusCode statusCode)
	{
		return Messenger.Format(
			provider: CultureInfo.CurrentCulture,
			format: Messages.Verbose_ReceivedResponse_Timed_Format,
			elapsedTime, statusCode.AsResponseString);
	}
	/// <summary>
	/// Writes the body of a request or a response to the debug stream.
	/// </summary>
	/// <remarks>
	/// The text goes to the host's UI as a debug line, so it can be written from any thread. When the host has no UI,
	/// nothing is written. The method doesn't check <see cref="CanDebugSerializeBefore"/> or
	/// <see cref="CanDebugSerializeAfter"/>; the caller does.
	/// </remarks>
	/// <param name="jsonPayload">The text to write, which describes the body and holds its JSON.</param>
	public void WriteDebugPayload(string jsonPayload)
	{
		if (this.Host?.UI is { } ui)
		{
			ui.WriteDebugLine(jsonPayload);
		}
	}
	/// <summary>
	/// Writes a verbose line about a request that is about to be sent, with its method and URL.
	/// </summary>
	/// <remarks>
	/// The line reads like <c>Sending GET request -&gt; http://host/api/v3/series</c>, and it goes to the host's UI, so it
	/// can be written from any thread. When the host has no UI, nothing is written. The method doesn't check
	/// <see cref="CanWriteVerbose"/>; the caller does.
	/// </remarks>
	/// <param name="request">The details of the pending request.</param>
	public void WriteVerboseBefore(IHttpRequestDetails request)
	{
		if (this.Host?.UI is { } ui)
		{
			string msg = GenerateBeforeMessage(request);
			ui.WriteVerboseLine(msg);
		}
	}
	/// <summary>
	/// Builds the verbose line that reports a request before it's sent.
	/// </summary>
	/// <param name="request">The details of the pending request.</param>
	/// <returns>The line, such as <c>Sending GET request -&gt; http://host/api/v3/series</c>.</returns>
	private static string GenerateBeforeMessage(IHttpRequestDetails request)
	{
		return $"Sending {request.RequestMethod} request -> {request.RequestUrl}";
	}

	/// <summary>
	/// Represents the outcome of a timed request, with the time it took and the services of the request's scope.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The value wraps the response that it's built from, and a <see langword="default"/> instance, which wraps none,
	/// reports an error without a record.
	/// </para>
	/// <para>TODO: Nothing in the solution creates this type. Confirm whether it's meant to be passed to code that
	/// handles a timed response, or removed.</para>
	/// </remarks>
	[DebuggerStepThrough]
	private readonly struct TimedResponse : ISonarrTimedResponse
	{
		readonly ISonarrResponse? _response;
		readonly TimeSpan _timeSpan;
		readonly IServiceProvider _provider;

		/// <summary>
		/// Initializes a new <see cref="TimedResponse"/> instance with the specified elapsed time, response, and service
		/// provider.
		/// </summary>
		/// <param name="elapsed">The time between when the request was sent and when the response was received.</param>
		/// <param name="response">The outcome of the request.</param>
		/// <param name="provider">The service provider of the request's scope.</param>
		internal TimedResponse(TimeSpan elapsed, ISonarrResponse response, IServiceProvider provider)
		{
			_response = response;
			_timeSpan = elapsed;
			_provider = provider;
		}

		/// <summary>
		/// Gets the time between when the request was sent and when the response was received.
		/// </summary>
		/// <value>The elapsed time of the request.</value>
		public TimeSpan Elapsed => _timeSpan;
		/// <summary>
		/// Gets the error record of the response.
		/// </summary>
		/// <value>
		/// The record that describes the failure, or <see langword="null"/> when the request succeeded or the instance
		/// wraps no response.
		/// </value>
		public SonarrErrorRecord? Error => _response?.Error;
		/// <summary>
		/// Gets a value that indicates whether the request failed.
		/// </summary>
		/// <value>
		/// <see langword="true"/> if the wrapped response reports an error or the instance wraps no response; otherwise,
		/// <see langword="false"/>.
		/// </value>
		public bool IsError => _response is null || _response.IsError;
		/// <summary>
		/// Gets a scoped service provider for code that handles the response.
		/// </summary>
		/// <value>The service provider of the request's scope.</value>
		public IServiceProvider Services => _provider;
		/// <summary>
		/// Gets the HTTP status code of the response.
		/// </summary>
		/// <value>
		/// The wrapped response's status code, or <see cref="HttpStatusCode.Unused"/> when the instance wraps no response.
		/// </value>
		public HttpStatusCode StatusCode => _response?.StatusCode ?? HttpStatusCode.Unused;
		/// <summary>
		/// Gets the URL that the response's request was sent to.
		/// </summary>
		/// <value>The wrapped response's request URL, or an empty string when the instance wraps no response.</value>
		public string RequestUrl => _response?.RequestUrl ?? string.Empty;
	}
}
