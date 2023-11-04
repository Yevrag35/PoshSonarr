﻿using MG.Sonarr.Next.Extensions;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;

namespace MG.Sonarr.Next.Shell.Cmdlets.Bases
{
    [DebuggerStepThrough]
    public abstract class SonarrMetadataCmdlet : SonarrApiCmdletBase
    {
        protected MetadataTag Tag { get; private set; } = MetadataTag.Empty;

        protected override void OnCreatingScope(IServiceProvider provider)
        {
            base.OnCreatingScope(provider);
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
