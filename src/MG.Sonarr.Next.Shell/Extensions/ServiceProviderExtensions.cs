using MG.Sonarr.Next.Metadata;
using Microsoft.Extensions.DependencyInjection;

namespace MG.Sonarr.Next.Shell.Extensions
{
    public static class ServiceProviderExtensions
    {
        public static MetadataTag GetMetadataTag(this IServiceProvider provider, [ConstantExpected] string tagValue)
        {
            ArgumentNullException.ThrowIfNull(provider);
            ArgumentException.ThrowIfNullOrEmpty(tagValue);

            return provider.GetRequiredService<IMetadataResolver>()[tagValue];
        }
    }
}
