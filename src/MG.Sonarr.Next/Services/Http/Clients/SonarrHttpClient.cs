using MG.Sonarr.Next.Collections;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models;
using MG.Sonarr.Next.Models.Errors;
using MG.Sonarr.Next.Services.Auth;
using MG.Sonarr.Next.Services.Http.Handlers;
using MG.Sonarr.Next.Services.Http.Requests;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.PowerShell.Commands;
using System.Management.Automation;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json.Nodes;

namespace MG.Sonarr.Next.Services.Http.Clients;

/// <summary>
/// An interface exposing HTTP methods for issuing RESTful requests to Sonarr's API v3 endpoints.
/// </summary>
public partial interface ISonarrClient
{
	SonarrClientResult SendDelete(string path, CancellationToken token = default);
	SonarrClientResult<T> SendGet<T>(string path, CancellationToken token = default);
	SonarrClientResult<TOutput> SendPost<TOutput>(string path, CancellationToken token = default);
	SonarrClientResult SendPost<T>(string path, T body, CancellationToken token = default) where T : notnull;
	SonarrClientResult<TOutput> SendPost<TBody, TOutput>(string path, TBody body, CancellationToken token = default) where TBody : notnull;
	SonarrClientResult SendPut<T>(string path, T body, CancellationToken token = default) where T : notnull;
	SonarrClientResult SendTest(CancellationToken token = default);
}

internal sealed partial class SonarrHttpClient : ISonarrClient
{
	const string TEST_API = "/system/status";

	readonly HttpClient _client;
	ISonarrJsonOptions _options;
	readonly IMetadataResolver _resolver;
	readonly IResponseReader _responseReader;
	readonly IServiceScopeFactory _scopeFactory;
	readonly IConnectionSettings _settings;

	public SonarrHttpClient(HttpClient client, IConnectionSettings settings, ISonarrJsonOptions options, IMetadataResolver resolver, IResponseReader reader, IServiceScopeFactory scopeFactory)
	{
		_client = client;
		_options = options;
		_resolver = resolver;
		_responseReader = reader;
		_settings = settings;
		_scopeFactory = scopeFactory;
	}

	public SonarrClientResult SendDelete(string path, CancellationToken token = default)
	{
		using ApiKeyRequestMessage request = new(HttpMethod.Delete, path, _scopeFactory);

		return this.SendNoResultRequest(request, path, token);
	}

	public SonarrClientResult<T> SendGet<T>(string path, CancellationToken token = default)
	{
		using ApiKeyRequestMessage request = new(HttpMethod.Get, path, _scopeFactory);

		var response = this.SendResultRequest<T>(request, path, token);
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
	public SonarrClientResult SendPost<T>(string path, T body, CancellationToken token = default) where T : notnull
	{
		using ApiKeyRequestMessage request = new(HttpMethod.Post, path, _scopeFactory);
		request.Content = JsonContent.Create(body, body.GetType(), options: _options.ForSerializing);

		return this.SendNoResultRequest(request, path, token);
	}
	public SonarrClientResult<TOutput> SendPost<TOutput>(string path, CancellationToken token = default)
	{
		using ApiKeyRequestMessage request = new(HttpMethod.Post, path, _scopeFactory);

		return this.SendResultRequest<TOutput>(request, path, token);
	}
	public SonarrClientResult<TOutput> SendPost<TBody, TOutput>(string path, TBody body, CancellationToken token = default) where TBody : notnull
	{
		using ApiKeyRequestMessage request = new(HttpMethod.Post, path, _scopeFactory);

		request.Content = JsonContent.Create(body, body.GetType(), options: _options.ForSerializing);

		var response = this.SendResultRequest<TOutput>(request, path, token);
		if (response.IsDataTaggable(out IJsonMetadataTaggable? taggable))
		{
			taggable.SetTag(_resolver);
		}

		return response;
	}
	public SonarrClientResult SendPut<T>(string path, T body, CancellationToken token = default) where T : notnull
	{
		using ApiKeyRequestMessage request = new(HttpMethod.Put, path, _scopeFactory);
		request.Content = JsonContent.Create(body, body.GetType(), options: _options.ForSerializing);

		return this.SendNoResultRequest(request, path, token);
	}
	public SonarrClientResult SendTest(CancellationToken token = default)
	{
		HttpResponseMessage? response = null;
		using TestRequestMessage request = new(TEST_API, _scopeFactory);

		try
		{

			response = _client.Send(request, token);
			if (response.IsSuccessStatusCode)
			{
				SonarrAuthType authType = TryParseResponse(
					response,
					options: _options.ForDeserializing,
					result: out SonarrStatus? status,
					disposeResponse: false,
					cancellationToken: token)
					&&
					status.TryGetAuthType(out SonarrAuthType parsedAuth)
						? parsedAuth : SonarrAuthType.None;
			}

			return response.IsSuccessStatusCode
				? new(response, TEST_API)
				: ParseMessage(TEST_API, response, token);
		}
		catch (Exception e)
		{
			return SonarrClientResult.FromException(e, ErrorCategory.ConnectionError, response?.StatusCode ?? (HttpStatusCode)599, response);
		}
		finally
		{
			response?.Dispose();
		}
	}

	private SonarrClientResult SendNoResultRequest(HttpRequestMessage request, string path, CancellationToken token)
	{
		HttpResponseMessage? response = null;
		try
		{
			response = _client.Send(request, token);
			HttpCall call = new(path, request, response);
			return _responseReader.ReadNoResultAsync(call, path, token)
				.GetAwaiter().GetResult();
		}
		catch (HttpRequestException httpEx)
		{
			_ = TryParseResponse(response, _options.ForDeserializing, out SonarrServerError? pso, disposeResponse: true, token);
			SonarrHttpException sonarrEx = new(request, response, ErrorCollection.FromOne(pso), httpEx);

			return SonarrClientResult.FromException(sonarrEx, ErrorCategory.InvalidResult, response?.StatusCode ?? ErrorHandler.NoResponseCode, response);
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

	private SonarrClientResult<T> SendResultRequest<T>(HttpRequestMessage request, string path, CancellationToken token)
	{
		HttpResponseMessage? response = null;

		try
		{
			response = _client.Send(request, token);
			return _responseReader.ReadResultAsync<T>(new(path, request, response), path, token)
				.GetAwaiter().GetResult();
		}
		catch (HttpRequestException httpEx)
		{
			_ = TryParseResponse(response, _options.ForDeserializing, out SonarrServerError? pso, disposeResponse: true, token);
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

	private static SonarrClientResult<T> ReturnFromException<T>(string path, HttpResponseMessage? response, ErrorCategory category, Exception e)
	{
		var result = SonarrClientResult.FromException<T>(e, category, response?.StatusCode ?? ErrorHandler.NoResponseCode, response);
		if (string.IsNullOrEmpty(result.RequestUrl))
		{
			result.RequestUrl = path;
		}

		return result;
	}

	private static SonarrClientResult ParseMessage(string url, HttpResponseMessage response, CancellationToken token)
	{
		JsonNode? node = JsonNode.Parse(response.Content.ReadAsStream(token));
		var result =
			SonarrClientResult.FromException(new HttpResponseException(node?.AsObject()["message"]?.ToJsonString(), response), ErrorCategory.ResourceUnavailable, response.StatusCode);

		if (string.IsNullOrEmpty(result.RequestUrl))
		{
			result.RequestUrl = url;
		}

		return result;
	}

	private static bool TryParseResponse<T>([NotNullWhen(true)] HttpResponseMessage? response, JsonSerializerOptions? options, [NotNullWhen(true)] out T? result, bool disposeResponse, CancellationToken cancellationToken)
		where T : class?
	{
		try
		{
			result = response?.Content.ReadFromJsonAsync<T>(options, cancellationToken).GetAwaiter().GetResult();
			return result is not null;
		}
		catch (Exception e)
		{
			Debug.Fail(e.Message);
			result = null;
			return false;
		}
		finally
		{
			if (disposeResponse)
			{
				response?.Dispose();
			}
		}
	}
}

public static class SonarrClientDependencyInjection
{
	internal const string API_HEADER_KEY = "X-Api-Key";

	internal static readonly ProductInfoHeaderValue UserAgent = new("PoshSonarr-Next", "2.0.0");

	private static IHttpClientBuilder AddSonarrClientInternal(
		IServiceCollection services,
		Assembly cmdletAssembly,
		IConnectionSettings settings,
		Action<IServiceProvider, JsonSerializerOptions> configureJson)
	{
		return services
				.AddMetadata(cmdletAssembly)
				.AddResponseReader()
				.AddSingleton(settings)
				//.AddSignalRClient()
				.AddSonarrDownloadClient()
				.AddSonarrJsonOptions(configureJson)
				.AddTransient<SonarrClientHandler>()
				.AddHttpClient<ISonarrClient, SonarrHttpClient>((provider, client) =>
				{
					var settings = provider.GetRequiredService<IConnectionSettings>();
					client.BaseAddress = settings.ServiceUri;
					client.Timeout = settings.Timeout;
					client.DefaultRequestHeaders
						.Add(API_HEADER_KEY, settings.ApiKey.GetValue());

					client.DefaultRequestHeaders.UserAgent.Add(UserAgent);

				})
				.ConfigurePrimaryHttpMessageHandler<SonarrClientHandler>();
	}
	public static IServiceCollection AddSonarrNonPSClient(this IServiceCollection services,
		IConnectionSettings settings,
		Action<IServiceProvider, JsonSerializerOptions> configureJson)
	{
		services.AddTransient<PathHandler>()
				.AddTransient<TestingHandler>();

		AddSonarrClientInternal(services, typeof(SonarrHttpClient).Assembly, settings, configureJson)
			.AddHttpMessageHandler<PathHandler>()
			.AddHttpMessageHandler<TestingHandler>();

		return services;
	}
	public static IServiceCollection AddSonarrClient(this IServiceCollection services,
		Assembly cmdletAssembly,
		IConnectionSettings settings,
		Action<IServiceProvider, JsonSerializerOptions> configureJson)
	{
		services.AddTransient<DebugSerializeHandler>()
				.AddTransient<ErrorHandler>()
				.AddTransient<PathHandler>()
				.AddTransient<TestingHandler>()
				.AddTransient<VerboseHandler>();

		AddSonarrClientInternal(services, cmdletAssembly, settings, configureJson)
			.AddHttpMessageHandler<PathHandler>()
			.AddHttpMessageHandler<VerboseHandler>()
			.AddHttpMessageHandler<DebugSerializeHandler>()
			.AddHttpMessageHandler<TestingHandler>()
			.AddHttpMessageHandler<ErrorHandler>();

		return services;
	}
}