using MG.Sonarr.Next.Services.Auth;
using MG.Sonarr.Next.Services.Http.Clients;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using System.Net;
using System.Text;

namespace MG.Sonarr.Next.Tests.Infrastructure;

/// <summary>
/// Stands in for a Sonarr instance: a connection's settings, and a primary handler that answers every request that the
/// connection's <see cref="ISonarrClient"/> sends, beneath the module's own handler chain.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="AddServices(IServiceCollection)"/> goes in a connection's service collection before the module's own
/// services, and its post-configuration of the typed client's options runs after theirs, so the fake handler replaces
/// the primary handler that the module registers, and every delegating handler of the module still runs.
/// </para>
/// <para>
/// It also registers a <see cref="ConnectionProbe"/> singleton and a <see cref="ScopeProbe"/> scoped service, which
/// write to <see cref="Log"/> when the connection's provider or a scope on it is disposed.
/// </para>
/// <para>
/// The type is public because the test cmdlets take it as a parameter.
/// </para>
/// </remarks>
public sealed class FakeSonarr
{
	/// <summary>
	/// The URL that every fake instance answers at. Nothing listens there, since no request leaves the process.
	/// </summary>
	internal static readonly Uri BaseUri = new("http://sonarr.test:8989/");

	private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _respond;

	/// <summary>
	/// Gets the name that the instance's probes write to the log.
	/// </summary>
	/// <value>The instance's name.</value>
	public string Name { get; }

	/// <summary>
	/// Gets the log that the instance's probes write to.
	/// </summary>
	/// <value>The log of the test that created the instance.</value>
	public ProbeLog Log { get; }

	/// <summary>
	/// Gets the settings of a connection to the instance.
	/// </summary>
	/// <value>The connection's settings.</value>
	public IConnectionSettings Settings { get; }

	/// <summary>
	/// Initializes a new <see cref="FakeSonarr"/> instance that answers with the specified function.
	/// </summary>
	/// <param name="name">The name that the instance's probes write to the log.</param>
	/// <param name="log">The log that the instance's probes write to.</param>
	/// <param name="respond">The function that answers each request, or throws as a failed send does.</param>
	/// <param name="timeout">The timeout of a connection's requests, or <see langword="null"/> for 30 seconds.</param>
	internal FakeSonarr(
		string name,
		ProbeLog log,
		Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond,
		TimeSpan? timeout = null)
	{
		this.Name = name;
		this.Log = log;
		_respond = respond;
		this.Settings = new TestConnectionSettings(BaseUri, timeout ?? TimeSpan.FromSeconds(30));
	}

	/// <summary>
	/// Creates an instance that answers every request with <c>200 OK</c> and an empty JSON object, or with no body for a
	/// <c>HEAD</c> request.
	/// </summary>
	/// <param name="name">The name that the instance's probes write to the log.</param>
	/// <param name="log">The log that the instance's probes write to.</param>
	/// <returns>The new instance.</returns>
	internal static FakeSonarr Ok(string name, ProbeLog log)
	{
		return new(name, log, (request, _) => Task.FromResult(Respond.Ok(request)));
	}

	/// <summary>
	/// Adds the fake primary handler and the probes to a connection's service collection.
	/// </summary>
	/// <param name="services">The connection's service collection.</param>
	internal void AddServices(IServiceCollection services)
	{
		_ = services.PostConfigure<HttpClientFactoryOptions>(
			typeof(ISonarrClient).Name,
			options => options.HttpMessageHandlerBuilderActions.Add(
				builder => builder.PrimaryHandler = new PrimaryHandler(_respond)));

		_ = services.AddSingleton(_ => new ConnectionProbe(this.Name, this.Log));
		_ = services.AddScoped(_ => new ScopeProbe(this.Name, this.Log));
	}

	/// <summary>
	/// Answers each request with the instance's function, on the asynchronous path and the synchronous one alike.
	/// </summary>
	private sealed class PrimaryHandler : HttpMessageHandler
	{
		private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _respond;

		internal PrimaryHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond)
		{
			_respond = respond;
		}

		protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			return _respond(request, cancellationToken).GetAwaiter().GetResult();
		}

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			return _respond(request, cancellationToken);
		}
	}
}

/// <summary>
/// Builds the responses that a <see cref="FakeSonarr"/> answers with.
/// </summary>
/// <remarks>
/// Each response carries its request, as one from the network does, since the module's handlers keep their flags for
/// a request in its options and reach them through the response.
/// </remarks>
internal static class Respond
{
	/// <summary>
	/// Creates a <c>200 OK</c> response with an empty JSON object, or with no body for a <c>HEAD</c> request.
	/// </summary>
	/// <param name="request">The request to answer.</param>
	/// <returns>The response.</returns>
	internal static HttpResponseMessage Ok(HttpRequestMessage request)
	{
		return request.Method == HttpMethod.Head
			? new HttpResponseMessage(HttpStatusCode.OK) { RequestMessage = request }
			: Json(request, HttpStatusCode.OK, "{}");
	}

	/// <summary>
	/// Creates a response with the specified status and JSON body.
	/// </summary>
	/// <param name="request">The request to answer.</param>
	/// <param name="status">The response's status code.</param>
	/// <param name="body">The response's JSON body.</param>
	/// <returns>The response.</returns>
	internal static HttpResponseMessage Json(HttpRequestMessage request, HttpStatusCode status, string body)
	{
		return new HttpResponseMessage(status)
		{
			RequestMessage = request,
			Content = new StringContent(body, Encoding.UTF8, "application/json"),
		};
	}
}

/// <summary>
/// Holds the settings of a connection to a <see cref="FakeSonarr"/>.
/// </summary>
internal sealed class TestConnectionSettings : IConnectionSettings
{
	/// <summary>
	/// The sandbox's fixed API key, which no real instance uses.
	/// </summary>
	private const string API_KEY = "0123456789abcdef0123456789abcdef";

	/// <inheritdoc/>
	public IApiKey ApiKey { get; } = new Key(API_KEY);

	/// <inheritdoc/>
	public SonarrAuthType AuthType { get; set; }

	/// <inheritdoc/>
	public bool NoApiInPath => false;

	/// <inheritdoc/>
	public Uri ServiceUri { get; }

	/// <inheritdoc/>
	public bool SkipCertValidation => false;

	/// <inheritdoc/>
	public TimeSpan Timeout { get; }

	/// <summary>
	/// Initializes a new <see cref="TestConnectionSettings"/> instance with the specified URL and timeout.
	/// </summary>
	/// <param name="serviceUri">The instance's URL.</param>
	/// <param name="timeout">The timeout of each request.</param>
	internal TestConnectionSettings(Uri serviceUri, TimeSpan timeout)
	{
		this.ServiceUri = serviceUri;
		this.Timeout = timeout;
	}

	/// <inheritdoc/>
	public bool TryGetProxy([NotNullWhen(true)] out IWebProxy? proxy)
	{
		proxy = null;
		return false;
	}

	/// <summary>
	/// Holds an API key.
	/// </summary>
	private sealed class Key : IApiKey
	{
		private readonly string _value;

		internal Key(string value)
		{
			_value = value;
		}

		public string GetValue()
		{
			return _value;
		}
	}
}
