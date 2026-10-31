using MG.Sonarr.Next.Collections;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Models.Errors;
using MG.Sonarr.Next.Services.Http.Handlers;
using MG.Sonarr.Next.Services.Http.Requests;
using System.Management.Automation;
using System.Net;
using System.Net.Http.Json;

namespace MG.Sonarr.Next.Services.Http.Clients;

public partial interface ISonarrClient
{
	/// <summary>
	/// Sends a DELETE request to the specified endpoint asynchronously.
	/// </summary>
	/// <param name="path">The endpoint's path, relative to the API.</param>
	/// <param name="token">A token that cancels the request.</param>
	/// <returns>A task that completes with the result of the request.</returns>
	Task<SonarrClientResult> SendDeleteAsync(string path, CancellationToken token = default);
	/// <summary>
	/// Sends a GET request to the specified endpoint asynchronously and deserializes the response's body.
	/// </summary>
	/// <remarks>
	/// A value that is an <see cref="IJsonMetadataTaggable"/> gets its metadata tag, and a value that is an
	/// <see cref="ISortable"/> with more than one item is sorted.
	/// </remarks>
	/// <typeparam name="T">The type to deserialize the response's body as.</typeparam>
	/// <param name="path">The endpoint's path, relative to the API.</param>
	/// <param name="token">A token that cancels the request.</param>
	/// <returns>A task that completes with the result of the request, which holds the deserialized value on success.</returns>
	Task<SonarrClientResult<T>> SendGetAsync<T>(string path, CancellationToken token = default);
	/// <summary>
	/// Sends a HEAD request to the server's <c>/ping</c> endpoint asynchronously.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The path takes the connection's URL base but not the API prefix, since the endpoint sits outside the API.
	/// </para>
	/// <para>
	/// An error status gives an error whose category follows the status: <see cref="ErrorCategory.ObjectNotFound"/> for
	/// 404, <see cref="ErrorCategory.PermissionDenied"/> for 403, <see cref="ErrorCategory.AuthenticationError"/> for 401,
	/// <see cref="ErrorCategory.ConnectionError"/> for a status of 500 or above, and
	/// <see cref="ErrorCategory.InvalidResult"/> for any other.
	/// </para>
	/// </remarks>
	/// <param name="token">A token that cancels the request.</param>
	/// <returns>A task that completes with the result of the request.</returns>
	Task<SonarrClientResult> SendPingAsync(CancellationToken token = default);
	/// <summary>
	/// Sends a POST request without a body to the specified endpoint asynchronously and deserializes the response's body.
	/// </summary>
	/// <typeparam name="T">The type to deserialize the response's body as.</typeparam>
	/// <param name="path">The endpoint's path, relative to the API.</param>
	/// <param name="token">A token that cancels the request.</param>
	/// <returns>A task that completes with the result of the request, which holds the deserialized value on success.</returns>
	Task<SonarrClientResult<T>> SendPostAsync<T>(string path, CancellationToken token = default);
	/// <summary>
	/// Sends a POST request with the specified body to the specified endpoint asynchronously.
	/// </summary>
	/// <typeparam name="T">The type of the body.</typeparam>
	/// <param name="path">The endpoint's path, relative to the API.</param>
	/// <param name="body">The object to send as the request's JSON body.</param>
	/// <param name="token">A token that cancels the request.</param>
	/// <returns>A task that completes with the result of the request.</returns>
	Task<SonarrClientResult> SendPostAsync<T>(string path, T body, CancellationToken token = default) where T : notnull;
	/// <summary>
	/// Sends a POST request with the specified body to the specified endpoint asynchronously and deserializes the
	/// response's body.
	/// </summary>
	/// <remarks>
	/// A value that is an <see cref="IJsonMetadataTaggable"/> gets its metadata tag.
	/// </remarks>
	/// <typeparam name="TBody">The type of the body.</typeparam>
	/// <typeparam name="TOutput">The type to deserialize the response's body as.</typeparam>
	/// <param name="path">The endpoint's path, relative to the API.</param>
	/// <param name="body">The object to send as the request's JSON body.</param>
	/// <param name="token">A token that cancels the request.</param>
	/// <returns>A task that completes with the result of the request, which holds the deserialized value on success.</returns>
	Task<SonarrClientResult<TOutput>> SendPostAsync<TBody, TOutput>(string path, TBody body, CancellationToken token = default) where TBody : notnull;
	/// <summary>
	/// Sends a PUT request with the specified body to the specified endpoint asynchronously.
	/// </summary>
	/// <typeparam name="T">The type of the body.</typeparam>
	/// <param name="path">The endpoint's path, relative to the API.</param>
	/// <param name="body">The object to send as the request's JSON body.</param>
	/// <param name="token">A token that cancels the request.</param>
	/// <returns>A task that completes with the result of the request.</returns>
	Task<SonarrClientResult> SendPutAsync<T>(string path, T body, CancellationToken token = default) where T : notnull;
}

internal sealed partial class SonarrHttpClient
{
	/// <inheritdoc/>
	public async Task<SonarrClientResult> SendDeleteAsync(string path, CancellationToken token = default)
	{
		using ApiKeyRequestMessage request = new(HttpMethod.Delete, path, _scopeFactory);

		return await this.SendNoResultRequestAsync(request, path, token).ConfigureAwait(false);
	}
	/// <inheritdoc/>
	public async Task<SonarrClientResult<T>> SendGetAsync<T>(string path, CancellationToken token = default)
	{
		using ApiKeyRequestMessage request = new(HttpMethod.Get, path, _scopeFactory);

		var response = await this.SendResultRequestAsync<T>(request, path, token).ConfigureAwait(false);
		if (response.IsDataTaggable(out IJsonMetadataTaggable? taggable))
		{
			taggable.SetTag(_resolver);
		}

		if (response.IsDataSortable(out ISortable? sortable) && sortable.Count > 1)
		{
			sortable.Sort();
		}

		return response;
	}
	//BUG: The send sits outside the try, so a refused connection, a timeout, or a cancellation throws out of the method instead of returning an error result.
	/// <inheritdoc/>
	public async Task<SonarrClientResult> SendPingAsync(CancellationToken token = default)
	{
		const string pingPath = "/ping";
		using ApiKeyRequestMessage request = new(HttpMethod.Head, pingPath, _scopeFactory);
		request.Options.TryAdd(PathHandler.Ping, true);
		_cmdletAccessor.AttachTo(request);

		using var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
		try
		{
			response.EnsureSuccessStatusCode();
			return new SonarrClientResult(response, pingPath);
		}
		catch (Exception e)
		{
			ErrorCategory category = response.StatusCode switch
			{
				HttpStatusCode.NotFound => ErrorCategory.ObjectNotFound,
				HttpStatusCode.Forbidden => ErrorCategory.PermissionDenied,
				HttpStatusCode.Unauthorized => ErrorCategory.AuthenticationError,
				>= HttpStatusCode.InternalServerError => ErrorCategory.ConnectionError,
				_ => ErrorCategory.InvalidResult,
			};

			return SonarrClientResult.FromException(e, category, response.StatusCode, response);
		}
	}
	/// <summary>
	/// Sends a POST request without a body to the specified endpoint asynchronously.
	/// </summary>
	/// <param name="path">The endpoint's path, relative to the API.</param>
	/// <param name="token">A token that cancels the request.</param>
	/// <returns>A task that completes with the result of the request.</returns>
	public async Task<SonarrClientResult> SendPostAsync(string path, CancellationToken token = default)
	{
		using ApiKeyRequestMessage request = new(HttpMethod.Post, path, _scopeFactory);

		return await this.SendNoResultRequestAsync(request, path, token).ConfigureAwait(false);
	}
	/// <inheritdoc/>
	public async Task<SonarrClientResult<T>> SendPostAsync<T>(string path, CancellationToken token = default)
	{
		using ApiKeyRequestMessage request = new(HttpMethod.Post, path, _scopeFactory);

		return await this.SendResultRequestAsync<T>(request, path, token).ConfigureAwait(false);
	}
	//BUG: It serializes the body by its declared type T, where every other method uses body.GetType(), so a body held as a base type, such as an AddSeriesObject passed as a SeriesObject, goes out through the base type's converter.
	/// <inheritdoc/>
	public async Task<SonarrClientResult> SendPostAsync<T>(string path, T body, CancellationToken token = default) where T : notnull
	{
		using ApiKeyRequestMessage request = new(HttpMethod.Post, path, _scopeFactory);
		string json = JsonSerializer.Serialize(body, _options.ForSerializing);

		//request.Content = JsonContent.Create(body, body.GetType(), options: _options.ForSerializing);
		request.Content = new StringContent(json, Encoding.UTF8, "application/json");

		return await this.SendNoResultRequestAsync(request, path, token).ConfigureAwait(false);
	}
	/// <inheritdoc/>
	public async Task<SonarrClientResult<TOutput>> SendPostAsync<TBody, TOutput>(string path, TBody body, CancellationToken token = default) where TBody : notnull
	{
		using ApiKeyRequestMessage request = new(HttpMethod.Post, path, _scopeFactory);

		request.Content = JsonContent.Create(body, body.GetType(), options: _options.ForSerializing);

		var response = await this.SendResultRequestAsync<TOutput>(request, path, token).ConfigureAwait(false);
		if (response.IsDataTaggable(out IJsonMetadataTaggable? taggable))
		{
			taggable.SetTag(_resolver);
		}

		return response;
	}
	/// <inheritdoc/>
	public async Task<SonarrClientResult> SendPutAsync<T>(string path, T body, CancellationToken token = default) where T : notnull
	{
		using ApiKeyRequestMessage request = new(HttpMethod.Put, path, _scopeFactory);
		request.Content = JsonContent.Create(body, body.GetType(), options: _options.ForSerializing);

		return await this.SendNoResultRequestAsync(request, path, token).ConfigureAwait(false);
	}

	//BUG: It has no catch for TaskCanceledException, unlike SendResultRequestAsync, so a timeout gets ConnectionError instead of OperationTimeout and a cancellation gets ConnectionError instead of OperationStopped.
	/// <summary>
	/// Sends the specified request asynchronously and reads its outcome as a request that returns no value.
	/// </summary>
	/// <remarks>
	/// The method stores the scope's cmdlet in the request's options before sending it, turns any exception into a failed
	/// result, and disposes the response before it completes.
	/// </remarks>
	/// <param name="request">The request to send.</param>
	/// <param name="path">The endpoint's path, which the result reports when no other URL is known.</param>
	/// <param name="token">A token that cancels the request.</param>
	/// <returns>A task that completes with the result of the request.</returns>
	private async Task<SonarrClientResult> SendNoResultRequestAsync(HttpRequestMessage request, string path, CancellationToken token)
	{
		HttpResponseMessage? response = null;
		_cmdletAccessor.AttachTo(request);
		try
		{
			response = await _client.SendAsync(request, token).ConfigureAwait(false);
			return await _responseReader.ReadNoResultAsync(new(path, request, response), path, token).ConfigureAwait(false);
		}
		catch (HttpRequestException httpEx)
		{
			_ = TryParseResponse(response, _options.ForDeserializing, out SonarrServerError? pso, disposeResponse: false, token);
			SonarrHttpException sonarrEx = new(request, response, ErrorCollection.FromOne(pso), httpEx);

			var result = SonarrClientResult.FromException(sonarrEx, ErrorCategory.InvalidResult, response?.StatusCode ?? ErrorHandler.NoResponseCode, response);
			if (string.IsNullOrEmpty(result.RequestUrl))
			{
				result.RequestUrl = path;
			}

			return result;
		}
		catch (Exception ex)
		{
			var result = SonarrClientResult.FromException(ex, ErrorCategory.ConnectionError, response?.StatusCode ?? ErrorHandler.NoResponseCode, response);
			if (string.IsNullOrEmpty(result.RequestUrl))
			{
				result.RequestUrl = path;
			}

			return result;
		}
		finally
		{
			response?.Dispose();
		}
	}
	/// <summary>
	/// Sends the specified request asynchronously and reads its outcome as a request that returns a value.
	/// </summary>
	/// <remarks>
	/// The method stores the scope's cmdlet in the request's options before sending it, turns any exception into a failed
	/// result, and disposes the response before it completes. A timeout gives the category
	/// <see cref="ErrorCategory.OperationTimeout"/>, and any other cancellation <see cref="ErrorCategory.OperationStopped"/>.
	/// </remarks>
	/// <typeparam name="T">The type to deserialize the response's body as.</typeparam>
	/// <param name="request">The request to send.</param>
	/// <param name="path">The endpoint's path, which the result reports when no other URL is known.</param>
	/// <param name="token">A token that cancels the request.</param>
	/// <returns>A task that completes with the result of the request, which holds the deserialized value on success.</returns>
	private async Task<SonarrClientResult<T>> SendResultRequestAsync<T>(HttpRequestMessage request, string path, CancellationToken token)
	{
		HttpResponseMessage? response = null;
		_cmdletAccessor.AttachTo(request);

		try
		{
			response = await _client.SendAsync(request, token).ConfigureAwait(false);
			return await _responseReader.ReadResultAsync<T>(new(path, request, response), path, token).ConfigureAwait(false);
		}
		catch (HttpRequestException httpEx)
		{
			_ = TryParseResponse(response, _options.ForDeserializing, out SonarrServerError? pso, disposeResponse: false, token);
			SonarrHttpException sonarrEx = new(request, response, ErrorCollection.FromOne(pso), httpEx);

			var result = SonarrClientResult.FromException<T>(sonarrEx, ErrorCategory.InvalidResult, response?.StatusCode ?? ErrorHandler.NoResponseCode, response);
			if (string.IsNullOrEmpty(result.RequestUrl))
			{
				result.RequestUrl = path;
			}

			return result;
		}
		catch (TaskCanceledException cancelled)
		{
			ErrorCategory cat = cancelled.InnerException is TimeoutException tout
				? ErrorCategory.OperationTimeout
				: ErrorCategory.OperationStopped;

			return ReturnFromException<T>(path, response, cat, cancelled);
		}
		catch (Exception e)
		{
			return ReturnFromException<T>(path, response, ErrorCategory.ConnectionError, e);
		}
		finally
		{
			response?.Dispose();
		}
	}
}
