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
        int _capacity;
        protected abstract int Capacity { get; }
        protected object[] Returnables => _objs ?? Array.Empty<object>();
        protected IPoolReturner Returner { get; private set; } = null!;
        protected MetadataTag Tag { get; private set; } = MetadataTag.Empty;

        protected override void OnCreatingScope(IServiceProvider provider)
        {
            base.OnCreatingScope(provider);
            this.Tag = this.GetMetadataTag(provider.GetRequiredService<MetadataResolver>());
            this.Returner = provider.GetRequiredService<IPoolReturner>();
            this.CreatingScopeAndCapacity(provider, this.Capacity);
        }
        protected void CreatingScopeAndCapacity(IServiceProvider provider, int capacity)
        {
            _capacity = capacity >= 0 ? capacity : 0;
            _objs = _capacity > 0 ? ArrayPool<object>.Shared.Rent(capacity) : Array.Empty<object>();
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
                if (_objs.Length > 0)
                {
                    this.Returner.Return(this.Returnables.AsSpan(0, _capacity));
                    ArrayPool<object>.Shared.Return(_objs);
                }

                _objs = null;
                _disposed = true;
            }

            base.Dispose(disposing);
        }
    }
}
