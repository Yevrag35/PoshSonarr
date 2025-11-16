using MG.Sonarr.Next.Metadata;

namespace MG.Sonarr.Next.Shell.Extensions;

public static class ServiceProviderExtensions
{
	public static MetadataTag GetMetadataTag(this IServiceProvider provider, [ConstantExpected] string tagValue)
	{
		return provider.GetRequiredService<IMetadataResolver>()[tagValue];
	}
}
