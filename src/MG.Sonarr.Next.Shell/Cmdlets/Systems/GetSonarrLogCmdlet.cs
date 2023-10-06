using MG.Sonarr.Next.Services.Http.Queries;
using MG.Sonarr.Next.Services.Metadata;
using MG.Sonarr.Next.Services.Models;
using MG.Sonarr.Next.Services.Models.System;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Shell.Extensions;
using System.ComponentModel;

namespace MG.Sonarr.Next.Shell.Cmdlets.Systems
{
    [Cmdlet(VerbsCommon.Get, "SonarrLog")]
    public sealed class GetSonarrLogCmdlet : SonarrMetadataCmdlet
    {
        protected override int Capacity => 1;
        QueryParameterCollection Parameters { get; set; } = null!;
        PagingParameter Paging { get; set; } = null!;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter]
        [ValidateRange(ValidateRangeKind.Positive)]
        [DefaultValue(10)]
        public int PageSize { get; set; }

        [Parameter]
        [Alias("Page")]
        [ValidateRange(ValidateRangeKind.Positive)]
        [DefaultValue(1)]
        public int PageNumber { get; set; }

        [Parameter]
        [Alias("Direction")]
        [DefaultValue(ListSortDirection.Descending)]
        public ListSortDirection SortDirection { get; set; }

        [Parameter]
        [DefaultValue("Time")]
        public string SortKey { get; set; } = string.Empty;

        protected override void OnCreatingScope(IServiceProvider provider)
        {
            base.OnCreatingScope(provider);
            this.Paging = this.GetPooledObject<PagingParameter>();
            this.Paging.SortKey = "Time";
            this.Returnables[0] = this.Paging;
            this.Parameters = new(2);
        }
        protected override void Begin(IServiceProvider provider)
        {
            this.SetPagingParams();
        }
        private void SetPagingParams()
        {
            if (this.HasParameter(x => x.PageNumber))
            {
                this.Paging.Page = this.PageNumber;
            }

            if (this.HasParameter(x => x.PageSize))
            {
                this.Paging.PageSize = this.PageSize;
            }

            if (this.HasParameter(x => x.SortDirection))
            {
                this.Paging.SortDirection = this.SortDirection;
            }

            if (this.HasParameter(x => x.SortKey))
            {
                this.Paging.SortKey = this.SortKey;
            }

            this.Parameters.AddOrUpdate(this.Paging);
        }

        protected override void Process(IServiceProvider provider)
        {
            this.WriteDebug(this.Paging.ToString(null, null));
            string url = this.Tag.GetUrl(this.Parameters);
            if (this.GetLogs(url, out var result))
            {
                this.WriteCollection(result.Records);
            }
        }

        private bool GetLogs(string url, [NotNullWhen(true)] out RecordResult<LogObject>? result)
        {
            var response = this.SendGetRequest<RecordResult<LogObject>>(url);
            if (response.IsError)
            {
                this.WriteError(response.Error);
                result = null;
                return false;
            }

            result = response.Data;
            return true;
        }

        protected override MetadataTag GetMetadataTag(MetadataResolver resolver)
        {
            return resolver[Meta.LOG_ITEM];
        }

        //bool _disposed;
        //protected override void Dispose(bool disposing)
        //{
        //    if (disposing && !_disposed)
        //    {

        //    }

        //    base.Dispose(disposing);
        //}
    }
}
