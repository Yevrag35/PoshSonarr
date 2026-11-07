using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Services.Auth;
using MG.Sonarr.Next.Shell.Cmdlets;
using Microsoft.Extensions.DependencyInjection;

namespace MG.Sonarr.Next.Tests.Infrastructure;

/// <summary>
/// Builds service providers with the services that a connection has, through the module's own builder, so the JSON
/// options, the HTTP client, and its handler chain are the ones that a cmdlet gets.
/// </summary>
/// <remarks>
/// The builder is the one that <c>Test-SonarrHost</c> calls, which doesn't touch the module's connection, so tests that
/// use these providers can run beside each other and beside the tests of the connection.
/// </remarks>
internal static class ModuleServices
{
	private static readonly Lazy<ServiceProvider> s_shared = new(() => Build(FakeSonarr.Ok("shared", new ProbeLog())));

	/// <summary>
	/// Gets the JSON options of a provider that every test shares, which a connection's converters are safe to be.
	/// </summary>
	/// <value>The JSON options that a connection reads and writes Sonarr's bodies with.</value>
	internal static ISonarrJsonOptions Json => s_shared.Value.GetRequiredService<ISonarrJsonOptions>();

	/// <summary>
	/// Gets the metadata resolver of the provider that every test shares.
	/// </summary>
	/// <value>The resolver that tags what a request returns.</value>
	internal static IMetadataResolver Resolver => s_shared.Value.GetRequiredService<IMetadataResolver>();

	/// <summary>
	/// Builds a provider with the services of a connection to the specified fake instance.
	/// </summary>
	/// <param name="sonarr">The instance whose settings, primary handler, and probes the provider takes.</param>
	/// <returns>A new provider, which the caller disposes.</returns>
	internal static ServiceProvider Build(FakeSonarr sonarr)
	{
		return new ProviderBuilder().Build(sonarr.Settings, sonarr.AddServices);
	}

	/// <summary>
	/// Reaches the module's builder through the base of <c>Test-SonarrHost</c>, without running a cmdlet.
	/// </summary>
	private sealed class ProviderBuilder : TestConnectionCmdlet
	{
		internal ServiceProvider Build(IConnectionSettings settings, Action<IServiceCollection> addServices)
		{
			return this.CreateTestContext(settings, addServices);
		}
	}
}
