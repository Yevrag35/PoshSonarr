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
    [Cmdlet(VerbsCommon.Remove, "SonarrEpisodeFile", ConfirmImpact = ConfirmImpact.High, SupportsShouldProcess = true)]
    [CmdletBinding(PositionalBinding = false)]
    [OutputType(typeof(void))]
    public class DeleteEpisodeFile : GetEpisodeFile
    {
        [Parameter(Mandatory = false)]
        public SwitchParameter Force { get; set; }

        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord() => base.ProcessRecord();

        protected override void EndProcessing()
        {
            var epFiles = GetResults().ToArray();
            for (int i = 0; i < epFiles.Length; i++)
            {
                var file = epFiles[i];
                if (ShouldProcess("Deleting episodeFileId " + file.Id, "Are you sure you want to delete this file?", "Delete EpisodeFile"))
                {
                    if (Force || ShouldContinue("This action is permanent!  Are you positive you want to delete this file?", "Delete File"))
                    {
                        var endpoint = new EpisodeFile(file.Id);
                        Api.SonarrDelete(endpoint);
                    }
                }
            }
        }
    }
}
