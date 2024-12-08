using MG.Http.Urls.Queries;
using MG.Sonarr.Next.Metadata;
using MG.Sonarr.Next.Models;
using MG.Sonarr.Next.Models.System;
using MG.Sonarr.Next.Services.Http.Queries;
using MG.Sonarr.Next.Shell.Cmdlets.Bases;
using MG.Sonarr.Next.Shell.Extensions;
using System.ComponentModel;

namespace MG.Sonarr.Next.Shell.Cmdlets.Systems.Logs
{
    [Cmdlet(VerbsCommon.Get, "SonarrLog")]
    public sealed class GetSonarrLogCmdlet : SonarrMetadataCmdlet
    {
        protected override int Capacity => 1;
        QueryParameterCollection _parameters = null!;
        PagingParameter _paging = null!;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        [Parameter]
        [ValidateRange(ValidateRangeKind.Positive)]
        [PSDefaultValue(Value = 10)]
        public int PageSize { get; set; }

        [Parameter]
        [Alias("Page")]
        [ValidateRange(ValidateRangeKind.Positive)]
        [PSDefaultValue(Value = 1)]
        public int PageNumber { get; set; }

        [Parameter]
        [Alias("Direction")]
        [PSDefaultValue(Value = ListSortDirection.Descending)]
        public ListSortDirection SortDirection { get; set; }

        [Parameter]
        [PSDefaultValue(Value = "Time")]
        public string SortKey { get; set; } = string.Empty;

        protected override void OnCreatingScope(IServiceProvider provider)
        {
            base.OnCreatingScope(provider);
            _paging = this.GetPooledObject<PagingParameter>();
            _paging.SortKey = "Time";
            this.GetReturnables()[0] = _paging;
            _parameters = new(2);
        }
        protected override void Begin(IServiceProvider provider)
        {
            this.SetPagingParams();
        }
        private void SetPagingParams()
        {
            if (this.HasParameter(x => x.PageNumber))
            {
                _paging.Page = this.PageNumber;
            }

            if (this.HasParameter(x => x.PageSize))
            {
                _paging.PageSize = this.PageSize;
            }

            if (this.HasParameter(x => x.SortDirection))
            {
                _paging.SortDirection = this.SortDirection;
            }

            if (this.HasParameter(x => x.SortKey))
            {
                _paging.SortKey = this.SortKey;
            }

            _parameters.AddOrUpdate(_paging);
        }

        protected override void Process(IServiceProvider provider)
        {
            this.WriteDebug(_paging.ToString(null, null));
            string url = this.Tag.GetUrl(_parameters);
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

        protected override MetadataTag GetMetadataTag(IMetadataResolver resolver)
        {
            return resolver[Meta.LOG_ITEM];
        }
    }
}
