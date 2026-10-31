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
using System.Text.Json.Nodes;

namespace MG.Sonarr.Next.Services.Http.Clients;

/// <summary>
/// Provides methods that send requests to the endpoints of Sonarr's v3 API.
/// </summary>
/// <remarks>
/// <para>
/// Each method takes the endpoint's path relative to the API and rooted, such as <c>/series</c> or <c>/system/status</c>,
/// and the handlers that the client sends through put the connection's URL base and API prefix in front of it.
/// </para>
/// <para>
/// No method throws for a failed request. A failure, whether the server answered with an error status, the response
/// couldn't be read, or no response arrived, comes back as a result whose <see cref="SonarrClientResult.IsError"/> is
/// <see langword="true"/> and whose <see cref="SonarrClientResult.Error"/> describes it.
/// </para>
/// <para>
/// A body is serialized as JSON by its runtime type, and a value is deserialized from JSON, with the connection's
/// <see cref="ISonarrJsonOptions"/>. The synchronous methods and their <c>Async</c> twins send through the same handlers.
/// </para>
/// <para>
/// An instance takes the cmdlet of the DI scope that resolves it and hands it to every request it sends, so that the
/// cmdlet receives the verbose and debug output of its own requests. Resolve the client from a scope, not from the root
/// provider.
/// </para>
/// </remarks>
public partial interface ISonarrClient
{
	/// <summary>
	/// Sends a DELETE request to the specified endpoint.
	/// </summary>
	/// <param name="path">The endpoint's path, relative to the API.</param>
	/// <param name="token">A token that cancels the request.</param>
	/// <returns>The result of the request.</returns>
	SonarrClientResult SendDelete(string path, CancellationToken token = default);
	/// <summary>
	/// Sends a GET request to the specified endpoint and deserializes the response's body.
	/// </summary>
	/// <remarks>
	/// A value that is an <see cref="IJsonMetadataTaggable"/> gets its metadata tag, and a value that is an
	/// <see cref="ISortable"/> with more than one item is sorted.
	/// </remarks>
	/// <typeparam name="T">The type to deserialize the response's body as.</typeparam>
	/// <param name="path">The endpoint's path, relative to the API.</param>
	/// <param name="token">A token that cancels the request.</param>
	/// <returns>The result of the request, which holds the deserialized value on success.</returns>
	SonarrClientResult<T> SendGet<T>(string path, CancellationToken token = default);
	/// <summary>
	/// Sends a POST request without a body to the specified endpoint and deserializes the response's body.
	/// </summary>
	/// <typeparam name="TOutput">The type to deserialize the response's body as.</typeparam>
	/// <param name="path">The endpoint's path, relative to the API.</param>
	/// <param name="token">A token that cancels the request.</param>
	/// <returns>The result of the request, which holds the deserialized value on success.</returns>
	SonarrClientResult<TOutput> SendPost<TOutput>(string path, CancellationToken token = default);
	/// <summary>
	/// Sends a POST request with the specified body to the specified endpoint.
	/// </summary>
	/// <typeparam name="T">The type of the body.</typeparam>
	/// <param name="path">The endpoint's path, relative to the API.</param>
	/// <param name="body">The object to send as the request's JSON body.</param>
	/// <param name="token">A token that cancels the request.</param>
	/// <returns>The result of the request.</returns>
	SonarrClientResult SendPost<T>(string path, T body, CancellationToken token = default) where T : notnull;
	/// <summary>
	/// Sends a POST request with the specified body to the specified endpoint and deserializes the response's body.
	/// </summary>
	/// <remarks>
	/// A value that is an <see cref="IJsonMetadataTaggable"/> gets its metadata tag.
	/// </remarks>
	/// <typeparam name="TBody">The type of the body.</typeparam>
	/// <typeparam name="TOutput">The type to deserialize the response's body as.</typeparam>
	/// <param name="path">The endpoint's path, relative to the API.</param>
	/// <param name="body">The object to send as the request's JSON body.</param>
	/// <param name="token">A token that cancels the request.</param>
	/// <returns>The result of the request, which holds the deserialized value on success.</returns>
	SonarrClientResult<TOutput> SendPost<TBody, TOutput>(string path, TBody body, CancellationToken token = default) where TBody : notnull;
	/// <summary>
	/// Sends a PUT request with the specified body to the specified endpoint.
	/// </summary>
	/// <typeparam name="T">The type of the body.</typeparam>
	/// <param name="path">The endpoint's path, relative to the API.</param>
	/// <param name="body">The object to send as the request's JSON body.</param>
	/// <param name="token">A token that cancels the request.</param>
	/// <returns>The result of the request.</returns>
	SonarrClientResult SendPut<T>(string path, T body, CancellationToken token = default) where T : notnull;
	/// <summary>
	/// Checks the connection's URL and API key by requesting the server's status, and stores the server's authentication
	/// type in the connection's settings.
	/// </summary>
	/// <remarks>
	/// On success, <see cref="IConnectionSettings.AuthType"/> takes the authentication type that the status names, or
	/// <see cref="SonarrAuthType.None"/> when the status names none that the module knows. On failure, the settings stay
	/// unchanged.
	/// </remarks>
	/// <param name="token">A token that cancels the request.</param>
	/// <returns>The result of the request.</returns>
	SonarrClientResult SendTest(CancellationToken token = default);
	/// <summary>
	/// Checks the connection's URL and API key by requesting the server's status, and deserializes the status.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The request goes to <c>/system/status</c>, which always answers with a JSON object, so a successful response whose
	/// body isn't one comes back as a 503 error that says the URL doesn't point at Sonarr's API.
	/// </para>
	/// <para>
	/// An error status gives an error with the category <see cref="ErrorCategory.ResourceUnavailable"/>, whose message is
	/// the <c>message</c> field of the response's JSON body, or else the status code and its name. A request that fails
	/// without an answer gives an error with the category <see cref="ErrorCategory.ConnectionError"/>.
	/// </para>
	/// <para>
	/// A value that is an <see cref="IJsonMetadataTaggable"/> gets its metadata tag.
	/// </para>
	/// </remarks>
	/// <typeparam name="T">The type to deserialize the status as.</typeparam>
	/// <param name="token">A token that cancels the request.</param>
	/// <returns>The result of the request, which holds the deserialized status on success.</returns>
	SonarrClientResult<T> SendTest<T>(CancellationToken token = default);
}

/// <summary>
/// Provides the <see cref="ISonarrClient"/> that sends a connection's requests through its typed
/// <see cref="HttpClient"/>.
/// </summary>
/// <remarks>
/// <para>
/// The container creates an instance for each resolution, around a new <see cref="HttpClient"/> that shares the
/// connection's handler chain. An instance holds no per-request state, but it carries the cmdlet of the scope that
/// resolved it, so it belongs to that scope.
/// </para>
/// <para>
/// Each method disposes its request and its response before it returns, so the response reader takes what the result
/// needs first.
/// </para>
/// </remarks>
internal sealed partial class SonarrHttpClient : ISonarrClient
{
	/// <summary>
	/// The path of the endpoint that <see cref="SendTest{T}(CancellationToken)"/> requests.
	/// </summary>
	const string TEST_API = "/system/status";

	/// <summary>
	/// The typed client that sends the requests.
	/// </summary>
	readonly HttpClient _client;
	/// <summary>
	/// The accessor that holds the cmdlet of the scope that resolved the client.
	/// </summary>
	readonly ApiCmdletAccessor _cmdletAccessor;
	/// <summary>
	/// The connection's JSON options, which serialize request bodies and deserialize response bodies.
	/// </summary>
	ISonarrJsonOptions _options;
	/// <summary>
	/// The resolver that tags the values that requests return.
	/// </summary>
	readonly IMetadataResolver _resolver;
	/// <summary>
	/// The reader that turns each exchange into a result.
	/// </summary>
	readonly IResponseReader _responseReader;
	/// <summary>
	/// The factory that each request message opens its scope from.
	/// </summary>
	readonly IServiceScopeFactory _scopeFactory;
	/// <summary>
	/// The connection's settings, which <see cref="SendTest(CancellationToken)"/> stores the authentication type in.
	/// </summary>
	readonly IConnectionSettings _settings;

	/// <summary>
	/// Initializes a new <see cref="SonarrHttpClient"/> instance with the specified typed client and the connection's
	/// services.
	/// </summary>
	/// <param name="client">The typed client that sends the requests.</param>
	/// <param name="settings">The connection's settings.</param>
	/// <param name="options">The connection's JSON options.</param>
	/// <param name="resolver">The resolver that tags the values that requests return.</param>
	/// <param name="reader">The reader that turns each exchange into a result.</param>
	/// <param name="cmdletAccessor">The accessor of the resolving scope, which holds the scope's cmdlet.</param>
	/// <param name="scopeFactory">The factory that each request message opens its scope from.</param>
	public SonarrHttpClient(HttpClient client, IConnectionSettings settings, ISonarrJsonOptions options, IMetadataResolver resolver, IResponseReader reader, ApiCmdletAccessor cmdletAccessor, IServiceScopeFactory scopeFactory)
	{
		_client = client;
		_cmdletAccessor = cmdletAccessor;
		_options = options;
		_resolver = resolver;
		_responseReader = reader;
		_settings = settings;
		_scopeFactory = scopeFactory;
	}

	/// <inheritdoc/>
	public SonarrClientResult SendDelete(string path, CancellationToken token = default)
	{
		using ApiKeyRequestMessage request = new(HttpMethod.Delete, path, _scopeFactory);

		return this.SendNoResultRequest(request, path, token);
	}

	/// <inheritdoc/>
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
	/// <inheritdoc/>
	public SonarrClientResult SendPost<T>(string path, T body, CancellationToken token = default) where T : notnull
	{
		using ApiKeyRequestMessage request = new(HttpMethod.Post, path, _scopeFactory);
		request.Content = JsonContent.Create(body, body.GetType(), options: _options.ForSerializing);

		return this.SendNoResultRequest(request, path, token);
	}
	/// <inheritdoc/>
	public SonarrClientResult<TOutput> SendPost<TOutput>(string path, CancellationToken token = default)
	{
		using ApiKeyRequestMessage request = new(HttpMethod.Post, path, _scopeFactory);

		return this.SendResultRequest<TOutput>(request, path, token);
	}
	/// <inheritdoc/>
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
	/// <inheritdoc/>
	public SonarrClientResult SendPut<T>(string path, T body, CancellationToken token = default) where T : notnull
	{
		using ApiKeyRequestMessage request = new(HttpMethod.Put, path, _scopeFactory);
		request.Content = JsonContent.Create(body, body.GetType(), options: _options.ForSerializing);

		return this.SendNoResultRequest(request, path, token);
	}
	/// <inheritdoc/>
	public SonarrClientResult SendTest(CancellationToken token = default)
	{
		SonarrClientResult<SonarrStatus> result = this.SendTest<SonarrStatus>(token);
		if (!result.IsError)
		{
			_settings.AuthType = result.Value.TryGetAuthType(out SonarrAuthType parsedAuth)
				? parsedAuth : SonarrAuthType.None;
		}

		return result;
	}
	/// <inheritdoc/>
	public SonarrClientResult<T> SendTest<T>(CancellationToken token = default)
	{
		HttpResponseMessage? response = null;
		using TestRequestMessage request = new(TEST_API, _scopeFactory);
		_cmdletAccessor.AttachTo(request);

		try
		{
			response = _client.Send(request, token);
			if (!response.IsSuccessStatusCode)
			{
				return ParseMessage<T>(TEST_API, response, token);
			}

			var result = _responseReader.ReadResultAsync<T>(new(TEST_API, request, response), TEST_API, token)
				.GetAwaiter().GetResult();

			if (result.IsDataTaggable(out IJsonMetadataTaggable? taggable))
			{
				taggable.SetTag(_resolver);
			}

			return result;
		}
		catch (Exception e)
		{
			return SonarrClientResult.FromException<T>(e, ErrorCategory.ConnectionError, response?.StatusCode ?? ErrorHandler.NoResponseCode, response);
		}
		finally
		{
			response?.Dispose();
		}
	}

	//BUG: It has no catch for TaskCanceledException, unlike SendResultRequest, so a timeout gets ConnectionError instead of OperationTimeout and a cancellation gets ConnectionError instead of OperationStopped.
	/// <summary>
	/// Sends the specified request synchronously and reads its outcome as a request that returns no value.
	/// </summary>
	/// <remarks>
	/// The method stores the scope's cmdlet in the request's options before sending it, turns any exception into a failed
	/// result, and disposes the response before it returns.
	/// </remarks>
	/// <param name="request">The request to send.</param>
	/// <param name="path">The endpoint's path, which the result reports when no other URL is known.</param>
	/// <param name="token">A token that cancels the request.</param>
	/// <returns>The result of the request.</returns>
	private SonarrClientResult SendNoResultRequest(HttpRequestMessage request, string path, CancellationToken token)
	{
		HttpResponseMessage? response = null;
		_cmdletAccessor.AttachTo(request);
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

	/// <summary>
	/// Sends the specified request synchronously and reads its outcome as a request that returns a value.
	/// </summary>
	/// <remarks>
	/// The method stores the scope's cmdlet in the request's options before sending it, turns any exception into a failed
	/// result, and disposes the response before it returns. A timeout gives the category
	/// <see cref="ErrorCategory.OperationTimeout"/>, and any other cancellation <see cref="ErrorCategory.OperationStopped"/>.
	/// </remarks>
	/// <typeparam name="T">The type to deserialize the response's body as.</typeparam>
	/// <param name="request">The request to send.</param>
	/// <param name="path">The endpoint's path, which the result reports when no other URL is known.</param>
	/// <param name="token">A token that cancels the request.</param>
	/// <returns>The result of the request, which holds the deserialized value on success.</returns>
	private SonarrClientResult<T> SendResultRequest<T>(HttpRequestMessage request, string path, CancellationToken token)
	{
		HttpResponseMessage? response = null;
		_cmdletAccessor.AttachTo(request);

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

	/// <summary>
	/// Creates the failed result of a request that returns a value from the specified exception.
	/// </summary>
	/// <typeparam name="T">The type of the value that the request would have returned.</typeparam>
	/// <param name="path">The endpoint's path, which the result reports when the exception and the response give no URL.</param>
	/// <param name="response">The response to the request, or <see langword="null"/> when there was none.</param>
	/// <param name="category">The category of the error.</param>
	/// <param name="e">The exception that made the request fail.</param>
	/// <returns>
	/// A failed result with the status code of <paramref name="response"/>, or <see cref="ErrorHandler.NoResponseCode"/>
	/// when there was no response.
	/// </returns>
	private static SonarrClientResult<T> ReturnFromException<T>(string path, HttpResponseMessage? response, ErrorCategory category, Exception e)
	{
		var result = SonarrClientResult.FromException<T>(e, category, response?.StatusCode ?? ErrorHandler.NoResponseCode, response);
		if (string.IsNullOrEmpty(result.RequestUrl))
		{
			result.RequestUrl = path;
		}

		return result;
	}

	/// <summary>
	/// Creates the failed result of a connection test whose response has an error status.
	/// </summary>
	/// <remarks>
	/// The error's message is the <c>message</c> field of the response's JSON body, or else the status code as
	/// <see cref="HttpStatusCodeExtensions.ToResponseString(HttpStatusCode)"/> writes it, such as <c>401 (Unauthorized)</c>.
	/// </remarks>
	/// <typeparam name="T">The type of the value that the test would have returned.</typeparam>
	/// <param name="url">The path of the test's endpoint, which the result reports when the response gives no URL.</param>
	/// <param name="response">The response with the error status.</param>
	/// <param name="token">A token that cancels reading the response's body.</param>
	/// <returns>A failed result with the category <see cref="ErrorCategory.ResourceUnavailable"/>.</returns>
	private static SonarrClientResult<T> ParseMessage<T>(string url, HttpResponseMessage response, CancellationToken token)
	{
		string message = ReadMessage(response, token) ?? response.StatusCode.ToResponseString();

		var result =
			SonarrClientResult.FromException<T>(new HttpResponseException(message, response), ErrorCategory.ResourceUnavailable, response.StatusCode, response);

		if (string.IsNullOrEmpty(result.RequestUrl))
		{
			result.RequestUrl = url;
		}

		return result;
	}

	/// <summary>
	/// Reads the <c>message</c> field from the JSON body of an error response.
	/// </summary>
	/// <remarks>
	/// Not every error response has a JSON body with a message: Sonarr answers a wrong API key with a 401 and an empty body,
	/// and a server in front of Sonarr can answer with a body that isn't JSON at all. Such a body gives
	/// <see langword="null"/> rather than an exception.
	/// </remarks>
	/// <param name="response">The error response whose body is read.</param>
	/// <param name="token">A token that cancels reading the body.</param>
	/// <returns>
	/// The message, or <see langword="null"/> when the body isn't a JSON object with a string <c>message</c> field.
	/// </returns>
	private static string? ReadMessage(HttpResponseMessage response, CancellationToken token)
	{
		try
		{
			JsonNode? node = JsonNode.Parse(response.Content.ReadAsStream(token));
			return node is System.Text.Json.Nodes.JsonObject obj && obj["message"] is JsonValue value && value.TryGetValue(out string? text)
				? text
				: null;
		}
		catch (JsonException)
		{
			return null;
		}
	}

	//BUG: A body that fails to deserialize, which the server controls, hits Debug.Fail, so a Debug build without a debugger ends the process instead of returning false.
	/// <summary>
	/// Attempts to deserialize the body of the specified response as the specified type.
	/// </summary>
	/// <typeparam name="T">The type to deserialize the body as.</typeparam>
	/// <param name="response">The response whose body is read, or <see langword="null"/>.</param>
	/// <param name="options">The JSON options that deserialize the body, or <see langword="null"/> for the defaults.</param>
	/// <param name="result">
	/// When this method returns, the deserialized value, or <see langword="null"/> when there's no response or the body
	/// doesn't deserialize.
	/// </param>
	/// <param name="disposeResponse">
	/// <see langword="true"/> to dispose <paramref name="response"/> before the method returns; otherwise,
	/// <see langword="false"/>.
	/// </param>
	/// <param name="cancellationToken">A token that cancels reading the body.</param>
	/// <returns>
	/// <see langword="true"/> if the body deserialized to a value that isn't <see langword="null"/>; otherwise,
	/// <see langword="false"/>.
	/// </returns>
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

/// <summary>
/// Provides the registrations of the module's Sonarr API clients and the services that they depend on.
/// </summary>
public static class SonarrClientDependencyInjection
{
	/// <summary>
	/// The name of the header that carries the API key.
	/// </summary>
	internal const string API_HEADER_KEY = "X-Api-Key";

	/// <summary>
	/// The user agent that every request sends.
	/// </summary>
	internal static readonly ProductInfoHeaderValue UserAgent = new("PoshSonarr-Next", "2.0.0");

	/// <summary>
	/// Registers the services that both kinds of client share, and the typed <see cref="ISonarrClient"/>, without its
	/// delegating handlers.
	/// </summary>
	/// <remarks>
	/// The registration adds the metadata tags, the response reader, the connection's settings, the cookie container that
	/// every primary handler of the connection shares, the scoped accessor of a scope's cmdlet, the download client, and
	/// the JSON options. The typed client takes the settings' service URI as its base address and their timeout as its
	/// timeout, and sends the API key and the module's user agent on every request.
	/// </remarks>
	/// <param name="services">The service collection to add the registrations to.</param>
	/// <param name="cmdletAssembly">The assembly whose cmdlets the metadata tags list as able to take piped objects.</param>
	/// <param name="settings">The connection's settings.</param>
	/// <param name="configureJson">A callback that configures the JSON options once they're built.</param>
	/// <returns>The builder of the typed client, which the caller adds the delegating handlers to.</returns>
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
				.AddSingleton(new CookieContainer())
				.AddScoped<ApiCmdletAccessor>()
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
	//BUG: It registers neither VerboseHandler nor DebugSerializeHandler, which the download client's handler chain resolves, so resolving ISonarrDownloadClient from this registration throws InvalidOperationException.
	/// <summary>
	/// Registers an <see cref="ISonarrClient"/> for a host outside PowerShell, with the services that it depends on.
	/// </summary>
	/// <remarks>
	/// The client sends through <see cref="PathHandler"/> and <see cref="TestingHandler"/> only, so its requests write no
	/// verbose or debug output and its error responses aren't parsed ahead of the response reader. The metadata tags come
	/// from this library's own assembly.
	/// </remarks>
	/// <param name="services">The service collection to add the registrations to.</param>
	/// <param name="settings">The connection's settings.</param>
	/// <param name="configureJson">A callback that configures the JSON options once they're built.</param>
	/// <returns><paramref name="services"/>, so that calls can be chained.</returns>
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
	/// <summary>
	/// Registers the module's <see cref="ISonarrClient"/> and <see cref="ISonarrDownloadClient"/>, with the services that
	/// they depend on.
	/// </summary>
	/// <remarks>
	/// <para>
	/// A request passes through <see cref="PathHandler"/>, <see cref="VerboseHandler"/>,
	/// <see cref="DebugSerializeHandler"/>, <see cref="TestingHandler"/>, and a handler that reads error responses, in that
	/// order, before the primary handler sends it.
	/// </para>
	/// <para>
	/// Both clients take the cmdlet of the scope that resolves them, so code resolves them from a scope rather than from
	/// the root provider.
	/// </para>
	/// </remarks>
	/// <param name="services">The service collection to add the registrations to.</param>
	/// <param name="cmdletAssembly">The assembly whose cmdlets the metadata tags list as able to take piped objects.</param>
	/// <param name="settings">The connection's settings.</param>
	/// <param name="configureJson">A callback that configures the JSON options once they're built.</param>
	/// <returns><paramref name="services"/>, so that calls can be chained.</returns>
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