using Sonarr.Api.Cmdlets.Base;
using Sonarr.Api.Endpoints;
using Sonarr.Api.Enums;
using Sonarr.Api.Results;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace Sonarr.Api.Cmdlets
{
    // This command does not work because the Sonarr-Api is expecting a 'SeriesId' along with the 'FileIds'.

    [Cmdlet(VerbsCommon.Rename, "SonarrFiles", SupportsShouldProcess = true)]
    [CmdletBinding(PositionalBinding = false)]
    public class RenameFiles : PipeableWithEpisodeCommand
    {
        private const string FILES = "files";
        private bool IsFileId = false;

        [Parameter(Mandatory = true, ParameterSetName = "ByFileIds")]
        public long[] FileIds { get; set; }

        internal override SonarrCommand Command => SonarrCommand.RenameFiles;

        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            if (!MyInvocation.BoundParameters.ContainsKey("FileIds") && !MyInvocation.BoundParameters.ContainsKey("Episode"))
                base.ProcessRecord();

            else if (!MyInvocation.BoundParameters.ContainsKey("FileIds"))
            {
                if (base.Episode.EpisodeFileId.HasValue)
                {
                    ids.Add(base.Episode.EpisodeFileId.Value);
                }
                IsFileId = true;
            }
            else
            {
                ids.AddRange(FileIds);
                IsFileId = true;
            }
        }

        protected override void EndProcessing()
        {
            if (!IsFileId && !IsEpisodeId)
            {
                for (int i = ids.Count - 1; i >= 0; i--)
                {
                    var id = ids[i];
                    var cmd = new Episode(id, IsEpisodeId);
                    var ep = Api.SonarrGetAs<EpisodeResult>(cmd);
                    foreach (EpisodeResult er in ep)
                    {
                        if (er.EpisodeFileId.HasValue)
                            ids.Add(er.EpisodeFileId.Value);
                    }
                    ids.Remove(id);
                }
            }

            if (Force || ShouldContinue("Perform 'Rename Files' on file ids: " + string.Join(", ", ids), "Are you sure?"))
            {
                var parameters = new Dictionary<string, object>(1)
                {
                    { FILES, ids }
                };
                var result = ProcessCommand(parameters);
                WriteObject(result);
            }
        }
    }
}
