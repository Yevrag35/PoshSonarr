using MG.Sonarr.Next.Metadata;

namespace MG.Sonarr.Next.Extensions.Services;

/// <summary>
/// Provides extension methods for <see cref="IServiceProvider"/>.
/// </summary>
internal static class ServiceProviderExtensions
{
	/// <summary>
	/// Gets a <see cref="MetadataTag"/> from the <see cref="IMetadataResolver"/> service using the specified tag value.
	/// </summary>
	/// <param name="provider">The service provider to extend.</param>
	extension(IServiceProvider provider)
	{
		/// <summary>
		/// Gets a <see cref="MetadataTag"/> from the <see cref="IMetadataResolver"/> service using the specified tag value.
		/// </summary>
		/// <param name="tagValue">The value of the metadata tag to retrieve.</param>
		/// <returns>The <see cref="MetadataTag"/> corresponding to the specified tag value.</returns>
		internal MetadataTag GetMetadataTag([ConstantExpected] string tagValue)
		{
			return provider.GetRequiredService<IMetadataResolver>()[tagValue];
		}
	}
}
