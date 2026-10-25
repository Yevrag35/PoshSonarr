using MG.Sonarr.Next.Collections;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Models.Errors;
using MG.Sonarr.Next.Services.Http.Handlers;
using MG.Sonarr.Next.Services.Http.Requests;
using System.Management.Automation;
using System.Net;
using System.Net.Http.Json;
using System.Text;

namespace MG.Sonarr.Next.Services.Http.Clients;

public partial interface ISonarrClient
{
	Task<SonarrClientResult> SendDeleteAsync(string path, CancellationToken token = default);
	Task<SonarrClientResult<T>> SendGetAsync<T>(string path, CancellationToken token = default);
	Task<SonarrClientResult> SendPingAsync(CancellationToken token = default);
	Task<SonarrClientResult<T>> SendPostAsync<T>(string path, CancellationToken token = default);
	Task<SonarrClientResult> SendPostAsync<T>(string path, T body, CancellationToken token = default) where T : notnull;
	Task<SonarrClientResult<TOutput>> SendPostAsync<TBody, TOutput>(string path, TBody body, CancellationToken token = default) where TBody : notnull;
	Task<SonarrClientResult> SendPutAsync<T>(string path, T body, CancellationToken token = default) where T : notnull;
}

internal sealed partial class SonarrHttpClient
{
	public Task<SonarrClientResult> SendDeleteAsync(string path, CancellationToken token = default)
	{
		using ApiKeyRequestMessage request = new(HttpMethod.Delete, path, _scopeFactory);

		return this.SendNoResultRequestAsync(request, path, token);
	}
	public async Task<SonarrClientResult<T>> SendGetAsync<T>(string path, CancellationToken token = default)
	{
		using ApiKeyRequestMessage request = new(HttpMethod.Get, path, _scopeFactory);

		var response = await this.SendResultRequestAsync<T>(request, path, token);
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
	public async Task<SonarrClientResult> SendPingAsync(CancellationToken token = default)
	{
		const string pingPath = "/ping";
		using ApiKeyRequestMessage request = new(HttpMethod.Head, pingPath, _scopeFactory);
		request.Options.TryAdd(PathHandler.Ping, true);

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
	public Task<SonarrClientResult> SendPostAsync(string path, CancellationToken token = default)
	{
		using ApiKeyRequestMessage request = new(HttpMethod.Post, path, _scopeFactory);

		return this.SendNoResultRequestAsync(request, path, token);
	}
	public Task<SonarrClientResult<T>> SendPostAsync<T>(string path, CancellationToken token = default)
	{
		using ApiKeyRequestMessage request = new(HttpMethod.Post, path, _scopeFactory);

		return this.SendResultRequestAsync<T>(request, path, token);
	}
	public Task<SonarrClientResult> SendPostAsync<T>(string path, T body, CancellationToken token = default) where T : notnull
	{
		using ApiKeyRequestMessage request = new(HttpMethod.Post, path, _scopeFactory);
		string json = JsonSerializer.Serialize(body, _options.ForSerializing);

		//request.Content = JsonContent.Create(body, body.GetType(), options: _options.ForSerializing);
		request.Content = new StringContent(json, Encoding.UTF8, "application/json");

		return this.SendNoResultRequestAsync(request, path, token);
	}
	public async Task<SonarrClientResult<TOutput>> SendPostAsync<TBody, TOutput>(string path, TBody body, CancellationToken token = default) where TBody : notnull
	{
		using ApiKeyRequestMessage request = new(HttpMethod.Post, path, _scopeFactory);

		request.Content = JsonContent.Create(body, body.GetType(), options: _options.ForSerializing);

		var response = await this.SendResultRequestAsync<TOutput>(request, path, token);
		if (response.IsDataTaggable(out IJsonMetadataTaggable? taggable))
		{
			taggable.SetTag(_resolver);
		}

		return response;
	}
	public Task<SonarrClientResult> SendPutAsync<T>(string path, T body, CancellationToken token = default) where T : notnull
	{
		using ApiKeyRequestMessage request = new(HttpMethod.Put, path, _scopeFactory);
		request.Content = JsonContent.Create(body, body.GetType(), options: _options.ForSerializing);

		return this.SendNoResultRequestAsync(request, path, token);
	}

	private async Task<SonarrClientResult> SendNoResultRequestAsync(HttpRequestMessage request, string path, CancellationToken token)
	{
		HttpResponseMessage? response = null;
		try
		{
			response = await _client.SendAsync(request, token);
			return await _responseReader.ReadNoResultAsync(new(path, request, response), path, token);
		}
		catch (HttpRequestException httpEx)
		{
			_ = TryParseResponse(response, _options.ForDeserializing, out SonarrServerError? pso, disposeResponse: false, token);
			SonarrHttpException sonarrEx = new(request, response, ErrorCollection.FromOne(pso), httpEx);

			var result = SonarrClientResult.FromException(sonarrEx, ErrorCategory.InvalidResult, response?.StatusCode ?? HttpStatusCode.Unused, response);
			if (string.IsNullOrEmpty(result.RequestUrl))
			{
				result.RequestUrl = path;
			}

			return result;
		}
		catch (Exception ex)
		{
			var result = SonarrClientResult.FromException(ex, ErrorCategory.ConnectionError, response?.StatusCode ?? HttpStatusCode.Unused, response);
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
	private async Task<SonarrClientResult<T>> SendResultRequestAsync<T>(HttpRequestMessage request, string path, CancellationToken token)
	{
		HttpResponseMessage? response = null;

		try
		{
			response = await _client.SendAsync(request, token);
			return await _responseReader.ReadResultAsync<T>(new(path, request, response), path, token);
		}
		catch (HttpRequestException httpEx)
		{
			_ = TryParseResponse(response, _options.ForDeserializing, out SonarrServerError? pso, disposeResponse: false, token);
			SonarrHttpException sonarrEx = new(request, response, ErrorCollection.FromOne(pso), httpEx);

			var result = SonarrClientResult.FromException<T>(sonarrEx, ErrorCategory.InvalidResult, response?.StatusCode ?? HttpStatusCode.Unused, response);
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
