using MG.Sonarr.Next.Extensions;
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
        QueryCol _parameters = null!;

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
        [ValidateRange((int)ListSortDirection.Ascending, (int)ListSortDirection.Descending)]
        [PSDefaultValue(Value = ListSortDirection.Descending)]
        public ListSortDirection SortDirection { get; set; } = ListSortDirection.Descending;

        [Parameter]
        [ValidateNotNullOrWhiteSpace]
        [PSDefaultValue(Value = "time")]
        public string SortKey { get; set; } = "time";

        protected override void OnCreatingScope(IServiceProvider provider)
        {
            base.OnCreatingScope(provider);

            _parameters = this.GetPooledObject<QueryCol>();

            this.SetReturnables(_parameters);
        }
        protected override void Begin(IServiceProvider provider)
        {
            this.SetPagingParams();
        }
        private void SetPagingParams()
        {
            if (this.HasParameter(this.SortKey))
            {
                
            }

            if (this.HasParameter(this.PageNumber))
            {
                _parameters.Add(PagingConstants.PageNumber, this.PageNumber);
            }

            if (this.HasParameter(this.PageSize))
            {
                _parameters.Add(PagingConstants.PageSize, this.PageSize);
            }

            _parameters.Add(PagingConstants.SortKey, this.SortKey);
            _parameters.Add(PagingConstants.SortDirection, this.SortDirection, this.SortDirection.GetLength());
        }

        protected override void Process(IServiceProvider provider)
        {
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
