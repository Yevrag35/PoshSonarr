using MG.Sonarr.Next.Services.Collections;
using MG.Sonarr.Next.Services.Extensions;
using MG.Sonarr.Next.Services.Json;
using MG.Sonarr.Next.Services.Metadata;
using Microsoft.Extensions.DependencyInjection;
using System.Buffers;

namespace MG.Sonarr.Next.Shell.Cmdlets.Bases
{
    public abstract class SonarrMetadataCmdlet : SonarrApiCmdletBase
    {
        object[]? _objs;
        readonly int _capacity;
        protected object[] Returnables => _objs ?? Array.Empty<object>();
        protected IPoolReturner Returner { get; }
        protected MetadataTag Tag { get; private set; } = MetadataTag.Empty;

        protected SonarrMetadataCmdlet(int capacity)
            : base()
        {
            _capacity = capacity;
            this.Tag = this.GetMetadataTag(this.Services.GetRequiredService<MetadataResolver>());
            this.Returner = this.Services.GetRequiredService<IPoolReturner>();
            _objs = ArrayPool<object>.Shared.Rent(capacity);
        }

        protected abstract MetadataTag GetMetadataTag(MetadataResolver resolver);
        protected MetadataList<T> GetAll<T>() where T : PSObject, IJsonMetadataTaggable
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

        bool _disposed;
        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed && _objs is not null)
            {
                this.Returner.Return(this.Returnables.AsSpan(0, _capacity));
                ArrayPool<object>.Shared.Return(_objs);
                _objs = null;
                _disposed = true;
            }

            base.Dispose(disposing);
        }
    }
}
