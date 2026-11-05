using MG.Sonarr.Next.Exceptions;
using MG.Sonarr.Next.Models;
using MG.Sonarr.Next.Services.Http;
using MG.Sonarr.Next.Services.Http.Clients;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Unions;

namespace MG.Sonarr.Next.Shell.Cmdlets;

/// <summary>
/// Provides the synchronous <c>Send*Request</c> helpers that Sonarr cmdlets send their HTTP requests to Sonarr API
/// endpoints through.
/// </summary>
/// <remarks>
/// <para>
/// Each helper starts the cmdlet's timer, sends the request through the <see cref="ISonarrClient"/> of the cmdlet's
/// scope, and blocks the calling thread until the result is read. The client puts the cmdlet in the request, so the
/// cmdlet's verbose and debug output reports the request and its response.
/// </para>
/// <para>
/// The helpers don't throw for a failed request. The result carries the error record instead, and the cmdlet decides
/// whether to write it. Unless a caller passes a token that can be canceled, a helper sends with
/// <see cref="SonarrCmdletBase.StoppingToken"/>, so stopping the pipeline, as Ctrl+C does, ends a request in flight,
/// and its result reports the error with the category <see cref="ErrorCategory.OperationStopped"/>.
/// </para>
/// </remarks>
[DebuggerStepThrough]
public abstract class SonarrApiCmdletBase : TimedCmdlet
{
	private ISonarrClient _client = null!;

	/// <summary>
	/// Runs the base class's setup for a new scope and then resolves the scope's <see cref="ISonarrClient"/>, which the
	/// <c>Send*Request</c> helpers send through.
	/// </summary>
	/// <remarks>
	/// It runs before <see cref="SonarrCmdletBase.OnCreatingScope(IServiceProvider)"/>, so a derived cmdlet can send
	/// requests from there.
	/// </remarks>
	/// <param name="provider">The scoped service provider of the cmdlet.</param>
	/// <exception cref="InvalidOperationException">Thrown when the scope has no <see cref="ISonarrClient"/>.</exception>
	private protected override void OnCreatingScopeInternal(IServiceProvider provider)
	{
		base.OnCreatingScopeInternal(provider);
		_client = provider.GetRequiredService<ISonarrClient>();
	}

	/// <summary>
	/// Gets the token that a request sends with: the specified one when it can be canceled, and
	/// <see cref="SonarrCmdletBase.StoppingToken"/> otherwise, so that stopping the pipeline ends the request.
	/// </summary>
	/// <param name="token">The token that the caller passed, or <see langword="default"/>.</param>
	/// <returns>The token to send the request with.</returns>
	private CancellationToken GetRequestToken(CancellationToken token)
	{
		return token.CanBeCanceled ? token : this.StoppingToken;
	}

	/// <summary>
	/// Sends a DELETE HTTP request to the specified Sonarr API endpoint.
	/// </summary>
	/// <param name="path">The relative URL path of the API endpoint.</param>
	/// <param name="token">A token that cancels the request, or <see langword="default"/> to cancel it when the pipeline is stopped.</param>
	/// <returns>
	///     A response object indicating the status code and any errors that may have occurred.
	/// </returns>
	protected SonarrClientResult SendDeleteRequest(string path, CancellationToken token = default)
	{
		this.StartTimer();

		return _client.SendDeleteAsync(path, this.GetRequestToken(token)).GetAwaiter().GetResult();
	}

	/// <summary>
	/// Sends a GET HTTP request to the specified Sonarr API endpoint and deserializes the response's body.
	/// </summary>
	/// <remarks>
	/// A value that is an <see cref="MG.Sonarr.Next.Json.IJsonMetadataTaggable"/> gets its metadata tag, and a list of more than one item
	/// that can be sorted is sorted.
	/// </remarks>
	/// <typeparam name="T">The type of object the response's content will be deserialized as.</typeparam>
	/// <param name="path">The relative URL path of the API endpoint.</param>
	/// <param name="token">A token that cancels the request, or <see langword="default"/> to cancel it when the pipeline is stopped.</param>
	/// <returns>
	///     A response object indicating the status code and any errors that may have occurred.
	/// </returns>
	protected SonarrClientResult<T> SendGetRequest<T>(string path, CancellationToken token = default)
	{
		this.StartTimer();
		return _client.SendGetAsync<T>(path, this.GetRequestToken(token)).GetAwaiter().GetResult();
	}

	/// <summary>
	/// Sends a POST HTTP request without a body to the specified Sonarr API endpoint and deserializes the response's
	/// body as <typeparamref name="T"/>.
	/// </summary>
	/// <typeparam name="T">The type of object to deserialize the response's content to.</typeparam>
	/// <param name="path">The endpoint's path, relative to the API.</param>
	/// <param name="token">A token that cancels the request, or <see langword="default"/> to cancel it when the pipeline is stopped.</param>
	/// <returns>The result of the request, which holds the deserialized value on success.</returns>
	protected SonarrClientResult<T> SendPostRequest<T>(string path, CancellationToken token = default)
	{
		this.StartTimer();
		return _client.SendPostAsync<T>(path, this.GetRequestToken(token)).GetAwaiter().GetResult();
	}
	/// <summary>
	/// Sends a POST request with the specified body to the specified Sonarr API endpoint.
	/// </summary>
	/// <param name="path">The endpoint's path, relative to the API.</param>
	/// <param name="body">The object to send as the request's JSON body, which is serialized by its runtime type.</param>
	/// <param name="token">A token that cancels the request, or <see langword="default"/> to cancel it when the pipeline is stopped.</param>
	/// <returns>The result of the request.</returns>
	protected SonarrClientResult SendPostRequest(string path, object body, CancellationToken token = default)
	{
		this.StartTimer();
		return _client.SendPostAsync(path, body, this.GetRequestToken(token)).GetAwaiter().GetResult();
	}
	/// <summary>
	/// Sends a POST request with the specified body to the specified Sonarr API endpoint and deserializes the response's
	/// body.
	/// </summary>
	/// <typeparam name="TOutput">The type to deserialize the response's body as.</typeparam>
	/// <param name="path">The endpoint's path, relative to the API.</param>
	/// <param name="body">The object to send as the request's JSON body, which is serialized by its runtime type.</param>
	/// <param name="token">A token that cancels the request, or <see langword="default"/> to cancel it when the pipeline is stopped.</param>
	/// <returns>The deserialized value on success; otherwise, the error record that describes the failure.</returns>
	protected Either<TOutput, SonarrErrorRecord> SendPostRequest<TOutput>(string path, object body, CancellationToken token = default)
	{
		this.StartTimer();
		SonarrClientResult<TOutput> response = _client.SendPostAsync<TOutput>(path, body, this.GetRequestToken(token)).GetAwaiter().GetResult();
		return !response.IsError
			? response.Value
			: response.Error;
	}
	/// <summary>
	/// Sends a PUT request with the specified body to the specified Sonarr API endpoint.
	/// </summary>
	/// <param name="path">The endpoint's path, relative to the API.</param>
	/// <param name="body">The object to send as the request's JSON body, which is serialized by its runtime type.</param>
	/// <param name="token">A token that cancels the request, or <see langword="default"/> to cancel it when the pipeline is stopped.</param>
	/// <returns>The result of the request.</returns>
	protected SonarrClientResult SendPutRequest(string path, object body, CancellationToken token = default)
	{
		this.StartTimer();
		return _client.SendPutAsync(path, body, this.GetRequestToken(token)).GetAwaiter().GetResult();
	}

	/// <summary>
	/// Commits the properties of the specified object when the response reports success, or resets them and writes the
	/// response's error when it reports a failure.
	/// </summary>
	/// <remarks>
	/// Serializing a model for a request body can change its properties, and committing or resetting it brings it back
	/// in step afterward. The error is written whether or not it's ignorable, and <c>-ErrorAction</c> decides what
	/// happens to it.
	/// </remarks>
	/// <typeparam name="T">The type of <see cref="SonarrObject"/> to process.</typeparam>
	/// <param name="sonarrObj">
	/// The <see cref="SonarrObject"/> whose properties can be
	/// either committed or reset.
	/// </param>
	/// <param name="response">
	/// The response from the Sonarr API call and whose
	/// <see cref="SonarrClientResult.IsError"/> property determines the action taken on 
	/// the properties of <paramref name="sonarrObj"/>.
	/// </param>
	/// <returns>
	/// <see langword="true"/> if <paramref name="sonarrObj"/> had its properties committed, indicating
	/// a successful operation; otherwise, <see langword="false"/> indicating an error occurred.
	/// </returns>
	protected bool TryCommitFromResponse<T>(T sonarrObj, SonarrClientResult response) where T : SonarrObject
	{
		if (response.IsError)
		{
			sonarrObj.Reset();
			this.WriteError(response.Error);
			return false;
		}
		else
		{
			sonarrObj.Commit();
			return true;
		}
	}
}