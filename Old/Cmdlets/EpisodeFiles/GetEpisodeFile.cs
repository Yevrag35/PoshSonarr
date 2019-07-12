using Sonarr.Api.Cmdlets.Base;
using Sonarr.Api.Endpoints;
using Sonarr.Api.Results;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace Sonarr.Api.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "SonarrEpisodeFile", ConfirmImpact = ConfirmImpact.None)]
    [CmdletBinding(PositionalBinding = false)]
    [OutputType(typeof(EpisodeFileResult))]
    public class GetEpisodeFile : PipeableWithSeries
    {
        private List<long> epIds;

        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = "ByEpisodeInput")]
        public EpisodeResult Episode { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "ByEpisodeFileIds")]
        public long[] FileId { get; set; }

        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            epIds = new List<long>();
        }

        protected override void ProcessRecord()
        {
            if (!MyInvocation.BoundParameters.ContainsKey("FileId") && !MyInvocation.BoundParameters.ContainsKey("Episode"))
                base.ProcessRecord();

            else if (MyInvocation.BoundParameters.ContainsKey("Episode") && this.Episode.EpisodeFileId.HasValue)
                epIds.Add(this.Episode.EpisodeFileId.Value);
        }

        protected override void EndProcessing()
        {
            var epFiles = GetResults();
            WriteObject(epFiles, true);
        }

        protected private IEnumerable<EpisodeFileResult> GetResults()
        {
            if (epIds.Count > 0)
                FileId = epIds.ToArray();

            return _list.Count > 0 ?
                GetResultsFromSeries(_list) : GetResultsFromIDs(FileId);
        }

        private List<EpisodeFileResult> GetResultsFromIDs(long[] ids)
        {
            var efList = new List<EpisodeFileResult>(ids.Length);
            for (int i = 0; i < ids.Length; i++)
            {
                var id = ids[i];
                var endpoint = new EpisodeFile(id, true);
                var result = Api.SonarrGetAs<EpisodeFileResult>(endpoint);
                efList.AddRange(result);
            }
            return efList;
        }

        private List<EpisodeFileResult> GetResultsFromSeries(List<SeriesResult> srs)
        {
            var efList = new List<EpisodeFileResult>();
            for (int i = 0; i < srs.Count; i++)
            {
                var ser = srs[i];
                var endpoint = new EpisodeFile(ser.Id, false);
                var result = Api.SonarrGetAs<EpisodeFileResult>(endpoint);
                efList.AddRange(result);
            }
            return efList;
        }
    }
}
