using MG.Sonarr.Next.Services.Http.Clients;
using MG.Sonarr.Next.Services.Http.Handlers;

namespace MG.Sonarr.Next.Services.Http;

/// <summary>
/// Provides the verbose and debug output that a cmdlet writes for each request that an <see cref="ISonarrClient"/> sends
/// on its behalf.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="VerboseHandler"/> and <see cref="DebugSerializeHandler"/> read the cmdlet from each request's options and
/// call these members around sending it.
/// </para>
/// <para>
/// The clients send each request synchronously, so the handlers call these members on the thread that sent the request,
/// which for a cmdlet is its pipeline thread. An implementation writes through the cmdlet's own <c>WriteVerbose</c> and
/// <c>WriteDebug</c>, so the lines reach the verbose and debug streams, where redirection captures them and a transcript
/// records them.
/// </para>
/// <para>
/// A write can throw: the cmdlet's own output throws a <see cref="PipelineStoppedException"/> under a preference of
/// <see cref="ActionPreference.Stop"/>, or when the user halts the cmdlet at an <see cref="ActionPreference.Inquire"/>
/// prompt. The clients let that exception through, so it ends the cmdlet's pipeline as it would from the cmdlet's own
/// code.
/// </para>
/// </remarks>
public interface IApiCmdlet
{
	/// <summary>
	/// Gets a value that indicates whether the request's JSON body is written to the debug stream before the request is sent.
	/// </summary>
	/// <value>
	/// <see langword="true"/> if the request body is written to the debug stream; otherwise, <see langword="false"/>.
	/// </value>
	bool CanDebugSerializeBefore { get; }
	/// <summary>
	/// Gets a value that indicates whether the response's JSON body is written to the debug stream after the response is
	/// received.
	/// </summary>
	/// <value>
	/// <see langword="true"/> if the response body is written to the debug stream; otherwise, <see langword="false"/>.
	/// </value>
	bool CanDebugSerializeAfter { get; }

	/// <summary>
	/// Gets a value that indicates whether the cmdlet writes verbose messages about its requests.
	/// </summary>
	/// <value>
	/// <see langword="true"/> if verbose messages are written; otherwise, <see langword="false"/>.
	/// </value>
	bool CanWriteVerbose { get; }

	/// <summary>
	/// Writes the body of a request or a response to the debug stream.
	/// </summary>
	/// <param name="jsonPayload">The text to write, which describes the body and holds its JSON.</param>
	void WriteDebugPayload(string jsonPayload);

	/// <summary>
	/// Writes a verbose message about a request that is about to be sent.
	/// </summary>
	/// <param name="request">The details of the pending request.</param>
	void WriteVerboseBefore(IHttpRequestDetails request);
	/// <summary>
	/// Writes a verbose message about the response to a request.
	/// </summary>
	/// <param name="response">The outcome of the request, with the response's status code and URL.</param>
	/// <param name="provider">A scoped service provider that the cmdlet can resolve services from.</param>
	/// <param name="options">
	/// The JSON options that deserialize the response, or <see langword="null"/> when there are none.
	/// </param>
	void WriteVerboseAfter(ISonarrResponse response, IServiceProvider provider, JsonSerializerOptions? options);
}
