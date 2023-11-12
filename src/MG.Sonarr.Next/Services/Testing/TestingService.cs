using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Services.Http;
using MG.Sonarr.Next.Services.Http.Clients;
using Microsoft.Extensions.DependencyInjection;

namespace MG.Sonarr.Next.Services.Testing
{
    public interface ITestingService
    {
        SonarrResponse SendTest<T>(string path, T resource, CancellationToken token = default) where T : ITestPipeable;
    }

    internal sealed class TestingService : ITestingService
    {
        readonly ISonarrClient _client;

        public TestingService(ISonarrClient client)
        {
            _client = client;
        }

        public SonarrResponse SendTest<T>(string path, T resource, CancellationToken token = default) where T : ITestPipeable
        {
            return _client.SendPost(path, resource, token);
        }
    }

    public static class TestingServiceDependencyInjection
    {
        public static IServiceCollection AddTestingService(this IServiceCollection services)
        {
            return services.AddScoped<ITestingService, TestingService>();
        }
    }
}
