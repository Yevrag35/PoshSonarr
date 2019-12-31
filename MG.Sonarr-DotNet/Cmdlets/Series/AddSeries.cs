using MG.Sonarr.Results;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using System.Reflection;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsCommon.Add, "Series", ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true, DefaultParameterSetName = "ByRootFolderPath")]
    [OutputType(typeof(SeriesResult))]
    [CmdletBinding(PositionalBinding = false)]
    public class AddSeries : BaseSonarrCmdlet
    {
        #region FIELDS/CONSTANTS
        private bool _iewf;
        private bool _iewof;
        private bool _nm;
        private bool _passThru;
        private bool _sfme;
        private bool _usf;

        #endregion

        #region PARAMETERS

        [Parameter(Mandatory = true, ParameterSetName = "ByFullPath")]
        public string FullPath { get; set; }

        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "ByRootFolderPath")]
        [Alias("path")]
        public string RootFolderPath { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter IgnoreEpisodesWithFiles
        {
            get => _iewf;
            set => _iewf = value;
        }

        [Parameter(Mandatory = false)]
        public SwitchParameter IgnoreEpisodesWithoutFiles
        {
            get => _iewof;
            set => _iewof = value;
        }

        [Parameter(Mandatory = false)]
        public SwitchParameter SearchForMissingEpisodes
        {
            get => _sfme;
            set => _sfme = value;
        }

        [Parameter(Mandatory = false)]
        public SwitchParameter PassThru
        {
            get => _passThru;
            set => _passThru = value;
        }

        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true)]
        public string Name { get; set; }

        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true)]
        public int TVDBId { get; set; }

        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true)]
        public int QualityProfileId { get; set; }

        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true)]
        public string TitleSlug { get; set; }

        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true)]
        public SeriesImage[] Images { get; set; }

        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true)]
        public ICollection<Season> Seasons { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter NotMonitored
        {
            get => _nm;
            set => _nm = value;
        }

        [Parameter(Mandatory = false)]
        public SwitchParameter UseSeasonFolders
        {
            get => _usf;
            set => _usf = value;
        }

        [Parameter(Mandatory = false, ValueFromPipelineByPropertyName = true)]
        public int TVRageId { get; set; }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            if ( ! base.HasParameterSpecified(this, x => x.UseSeasonFolders) && this.Seasons != null && this.Seasons.Count > 1)
                _usf = true;

            if (this.QualityProfileId == 0)  // There is never a profile with an ID of 0...
                this.QualityProfileId = 1;

            SonarrBodyParameters postJson = this.GetParameters();

            if (base.HasParameterSpecified(this, x => x.TVRageId))
            {
                postJson.Add("tvRageId", this.TVRageId);
            }

            if (!string.IsNullOrEmpty(this.RootFolderPath))
            {
                postJson.Add("rootFolderPath", this.RootFolderPath);
            }
            else
            {
                postJson.Add("path", this.FullPath);
            }

            base.WriteDebug("POST BODY:" + Environment.NewLine + postJson.ToJson());

            if (base.ShouldProcess(string.Format("New Series - {0}", this.Name, "Adding")))
            {
                base.WriteVerbose(string.Format("Adding new series - {0} at \"/series\"", this.Name));
                SeriesResult sr = base.SendSonarrPost<SeriesResult>("/series", postJson);
                if (_passThru)
                    base.SendToPipeline(sr);
            }
        }

        #endregion

        #region BACKEND METHODS
        private SonarrBodyParameters GetParameters()
        {
            return new SonarrBodyParameters(9)
            {
                { "images", this.Images },
                { "monitored", !_nm },
                { "qualityProfileId", this.QualityProfileId },
                { "seasonFolder", _usf },
                { "seasons", this.Seasons },
                { "title", this.Name },
                { "titleSlug", this.TitleSlug },
                { "tvdbId", this.TVDBId },
                { "addOptions", new SonarrBodyParameters(3)
                    {
                        { "ignoreEpisodesWithFiles", _iewf },
                        { "ignoreEpisodesWithoutFiles", _iewof },
                        { "searchForMissingEpisodes", _sfme }
                    }
                }
            };
        }

        #endregion
    }
}