using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Services.Http;
using MG.Sonarr.Next.Services.Http.Clients;
using Microsoft.Extensions.DependencyInjection;

namespace MG.Sonarr.Next.Services.Testing;

/// <summary>
/// Defines operations that submit test payloads to Sonarr endpoints.
/// </summary>
public interface ITestingService
{
	/// <summary>
	/// Sends the specified test resource to the provided API path.
	/// </summary>
	/// <typeparam name="T">The test resource type.</typeparam>
	/// <param name="path">The API path that receives the test payload.</param>
	/// <param name="resource">The test resource to submit.</param>
	/// <param name="token">The cancellation token for the request.</param>
	/// <returns>The result of the test request.</returns>
	SonarrClientResult SendTest<T>(string path, T resource, CancellationToken token = default) where T : ITestPipeable;
}

/// <summary>
/// Represents the default implementation of <see cref="ITestingService"/>.
/// </summary>
internal sealed class TestingService : ITestingService
{
	readonly ISonarrClient _client;

	/// <summary>
	/// Initializes a new <see cref="TestingService"/> instance with the specified Sonarr client.
	/// </summary>
	/// <param name="client">The Sonarr client used to submit test requests.</param>
	public TestingService(ISonarrClient client)
	{
		_client = client;
	}

	/// <inheritdoc cref="ITestingService.SendTest{T}(string, T, CancellationToken)"/>
	public SonarrClientResult SendTest<T>(string path, T resource, CancellationToken token = default) where T : ITestPipeable
	{
		return _client.SendPost(path, resource, token);
	}
}

/// <summary>
/// Provides dependency injection registrations for testing services.
/// </summary>
public static class TestingServiceDependencyInjection
{
	/// <summary>
	/// Adds the testing service to the specified service collection.
	/// </summary>
	/// <param name="services">The service collection to update.</param>
	/// <returns>The same <see cref="IServiceCollection"/> instance so that additional calls can be chained.</returns>
	public static IServiceCollection AddTestingService(this IServiceCollection services)
	{
		return services.AddScoped<ITestingService, TestingService>();
	}
}
