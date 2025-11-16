using MG.Sonarr.Next.Collections;
using MG.Sonarr.Next.Services.Auth;
using MG.Sonarr.Next.Services.Http.Handlers;
using MG.Sonarr.Next.Services.Http.Requests;
using Microsoft.Extensions.DependencyInjection;
using System.Management.Automation;
using System.Net;

namespace MG.Sonarr.Next.Services.Http.Clients;

public interface ISonarrDownloadClient
{
	Task<SonarrClientResult<string>> DownloadToPathAsync(string url, string path, CancellationToken token = default);
	Task<SonarrClientResult<string>> DownloadToPathAsync(string url, string path, NetworkCredential? credential, CancellationToken token = default);
}

internal sealed class SonarrDownloadClient : ISonarrDownloadClient
{
	readonly HttpClient _client;
	readonly IServiceScopeFactory _scopeFactory;

	public SonarrDownloadClient(HttpClient client, IServiceScopeFactory scopeFactory)
	{
		_client = client;
		_scopeFactory = scopeFactory;
	}

	public async Task<SonarrClientResult<string>> DownloadToPathAsync(string url, string path, CancellationToken token = default)
	{
		using ApiKeyRequestMessage msg = new(HttpMethod.Get, url, _scopeFactory);

		HttpResponseMessage response = null!;
		try
		{
			try
			{
				response = await _client.SendAsync(msg, token).ConfigureAwait(false);
			}
			catch (SonarrHttpException ex)
			{
				var result = SonarrClientResult.FromException<string>(ex, ErrorCategory.InvalidResult, response?.StatusCode ?? ErrorHandler.NoResponseCode, response);
				if (string.IsNullOrEmpty(result.RequestUrl))
				{
					result.RequestUrl = url;
				}

				return result;
			}

			bool written = await WriteFileAsync(response, path, token).ConfigureAwait(false);

			return written
				? SonarrClientResult.Create(path, response, url)
				: SonarrClientResult.FromException<string>(new SonarrHttpException(msg, response, ErrorCollection.Empty, null), ErrorCategory.WriteError, response.StatusCode, response);
		}
		finally
		{
			response?.Dispose();
		}
	}
	public async Task<SonarrClientResult<string>> DownloadToPathAsync(string url, string path, NetworkCredential? credential, CancellationToken token = default)
	{
		using AuthedRequestMessage msg = new(HttpMethod.Get, url, credential, _scopeFactory);

		HttpResponseMessage response = null!;
		try
		{
			response = await _client.SendAsync(msg, token).ConfigureAwait(false);
		}
		catch (SonarrHttpException ex)
		{
			var result = SonarrClientResult.FromException<string>(ex, ErrorCategory.InvalidResult, response?.StatusCode ?? ErrorHandler.NoResponseCode, response);
			if (string.IsNullOrEmpty(result.RequestUrl))
			{
				result.RequestUrl = url;
			}

			return result;
		}

		bool written = await WriteFileAsync(response, path, token).ConfigureAwait(false);

		return written
			? SonarrClientResult.Create(path, response, url)
			: SonarrClientResult.FromException<string>(new SonarrHttpException(msg, response, ErrorCollection.Empty, null), ErrorCategory.WriteError, response.StatusCode, response);
	}

	private static async Task<bool> WriteFileAsync(HttpResponseMessage response, string path, CancellationToken token)
	{
		Stream stream = response.Content.ReadAsStream(token);
		await using (stream.ConfigureAwait(false))
		{
			if (stream.CanSeek && stream.Length <= 0)
			{
				return false;
			}

			FileStream fs = new(path, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 8192, useAsync: true);
			await using (fs.ConfigureAwait(false))
			{
				await stream.CopyToAsync(fs, token).ConfigureAwait(false);
				return true;
			}
		}
	}
}

internal static class SonarrDownloadClientDependencyInjection
{
	internal static IServiceCollection AddSonarrDownloadClient(this IServiceCollection services)
	{
		services
			.AddTransient<AuthHandler>()
			.AddHttpClient<ISonarrDownloadClient, SonarrDownloadClient>((provider, client) =>
			{
				var settings = provider.GetRequiredService<IConnectionSettings>();
				client.BaseAddress = settings.ServiceUri;
				client.Timeout = settings.Timeout;
				client.DefaultRequestHeaders
					.Add(SonarrClientDependencyInjection.API_HEADER_KEY, settings.ApiKey.GetValue());

				client.DefaultRequestHeaders.UserAgent
					.Add(SonarrClientDependencyInjection.UserAgent);
			})
			.ConfigurePrimaryHttpMessageHandler<SonarrClientHandler>()
			.AddHttpMessageHandler<VerboseHandler>()
			.AddHttpMessageHandler<DebugSerializeHandler>()
			.AddHttpMessageHandler<AuthHandler>();

		return services;
	}
}