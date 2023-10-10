using MG.Sonarr.Next.Metadata;

namespace MG.Sonarr.Next.Json
{
    /// <summary>
    /// An interface allowing an implementation to find a <see cref="MetadataTag"/> that correctly identifies
    /// itself from a provided resolver class.
    /// </summary>
    public interface IJsonMetadataTaggable
    {
        /// <summary>
        /// Find and sets the current instance's <see cref="MetadataTag"/> instance to one that correctly identifies
        /// the implementation being of that type.
        /// </summary>
        /// <param name="resolver">The resolver collection to search for tags in.</param>
        void SetTag(IMetadataResolver resolver);
    }

    /// <summary>
    /// An interface exposing a method and property for finding and storing their own <see cref="MetadataTag"/> 
    /// from a given resolver collection.
    /// </summary>
    public interface IJsonSonarrMetadata : IJsonMetadataTaggable
    {
        /// <summary>
        /// The tag that represents the type of data the current object holds and what Sonarr endpoint it may have
        /// come from.
        /// </summary>
        MetadataTag MetadataTag { get; }
    }
}
