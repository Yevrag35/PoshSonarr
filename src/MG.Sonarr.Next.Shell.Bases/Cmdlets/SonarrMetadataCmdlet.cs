using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;

namespace MG.Sonarr.Next.Shell.Cmdlets.Bases
{
    /// <summary>
    /// An <see langword="abstract"/>, <see cref="SonarrApiCmdletBase"/> class that provides an 
    /// implementation for cmdlets that query/manipulate specific metadata types returned from or provided to
    /// the Sonarr APIs.
    /// </summary>
    [DebuggerStepThrough]
    public abstract class SonarrMetadataCmdlet : SonarrApiCmdletBase
    {
        MetadataTag? _tag;

        /// <summary>
        /// Gets the defined <see cref="MetadataTag"/> that this cmdlet deals with when querying
        /// Sonarr APIs.
        /// </summary>
        protected MetadataTag Tag
        {
            get => _tag ??= MetadataTag.Empty;
            private set => _tag = value;
        }

        private protected sealed override void OnCreatingScopeInternal(IServiceProvider provider)
        {
            base.OnCreatingScopeInternal(provider);
            this.Tag = this.GetMetadataTag(provider.GetRequiredService<IMetadataResolver>());
        }

        protected abstract MetadataTag GetMetadataTag(IMetadataResolver resolver);
        protected MetadataList<T> GetAll<T>() where T : PSObject, IComparable<T>, IJsonMetadataTaggable
        {
            var response = this.SendGetRequest<MetadataList<T>>(this.Tag.UrlBase);
            if (response.IsError)
            {
                this.StopCmdlet(response.Error);
                return new();
            }

            return response.Data;
        }
        protected IEnumerable<T> GetById<T>(IReadOnlyCollection<int>? ids) where T : PSObject
        {
            if (ids.IsNullOrEmpty())
            {
                yield break;
            }

            foreach (int id in ids)
            {
                string url = this.Tag.GetUrlForId(id);
                var response = this.SendGetRequest<T>(url);
                if (response.IsError)
                {
                    this.WriteConditionalError(response.Error);
                }
                else
                {
                    yield return response.Data;
                }
            }
        }
    }
}
