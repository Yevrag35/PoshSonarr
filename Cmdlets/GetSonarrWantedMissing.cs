using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MG.Api;
using Sonarr.Api.Cmdlets.Base;
using Sonarr.Api.Components;
using Sonarr.Api.Results;
using Sonarr.Api.Enums;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace Sonarr.Api.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "SonarrWantedMissing", ConfirmImpact = ConfirmImpact.None)]
    [CmdletBinding(PositionalBinding = false)]
    [OutputType(typeof(EpisodeResult))]
    public class GetSonarrWantedMissing : BaseCmdlet
    {
        [Parameter(Mandatory = false)]
        public SortKey SortKey = SortKey.SeriesTitle;

        [Parameter(Mandatory = false)]
        public SortDirection SortDirection = SortDirection.Ascending;

        [Parameter(Mandatory = false, DontShow = true)]
        public int StartAtPage = 1;

        [Parameter(Mandatory = false)]
        [ValidateRange(1, 65536)]
        public int ResultSize = 10;

        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            var wm = new Endpoints.WantedMissing(SortKey, SortDirection, StartAtPage, ResultSize);
            var result = Api.SonarrGetAs<WantedMissingResult>(wm).ToArray()[0];
            WriteObject(result);
        }
    }
}
