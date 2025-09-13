using MG.Sonarr.Next.Collections;
using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models;
using MG.Sonarr.Next.Services.Http;

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
        protected MetadataList<T> GetAll<T>(string? url = null) where T : PSObject, IComparable<T>, IJsonMetadataTaggable
        {
            var response = this.SendGetRequest<MetadataList<T>>(url ?? this.Tag.UrlBase);
            if (response.IsError)
            {
                this.StopCmdlet(response.Error);
                return [];
            }

            return response.Data;
        }
        protected MetadataList<T> GetAllAndFilter<T>(SortedSet<int> ids, WildcardSet names, string? url = null) where T : PSObject, IComparable<T>, IHasId, IHasName, IJsonMetadataTaggable
        {
            MetadataList<T> list = this.GetAll<T>(url);
            if (list.Count == 0)
            {
                return list;
            }

            ReadOnlySpan<T> span = list.AsSpan();
            for (int i = span.Length - 1; i >= 0; i--)
            {
                ref readonly T item = ref span[i];
                if (!ids.Contains(item.Id) && !names.IsAnyMatch(item.Name))
                {
                    list.RemoveAt(i);
                }
            }

            return list;
        }
        protected List<T> GetById<T>(IReadOnlyCollection<int>? ids) where T : PSObject
        {
            if (ids.IsNullOrEmpty())
            {
                return [];
            }

            List<T> list = new(ids.Count);

            foreach (int id in ids)
            {
                string url = this.Tag.GetUrlForId(id);
                SonarrResponse<T> response = this.SendGetRequest<T>(url);
                if (response.IsError)
                {
                    this.WriteConditionalError(response.Error);
                    continue;
                }
                
                list.Add(response.Data);
            }

            return list;
        }
    }
}
