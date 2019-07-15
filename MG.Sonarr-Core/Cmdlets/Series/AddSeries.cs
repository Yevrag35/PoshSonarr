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


        #endregion

        #region PARAMETERS
        //[Parameter(Mandatory = true, ValueFromPipeline = true, Position = 0)]
        //[Alias("InputObject")]
        //public SeriesResult Series { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "ByFullPath")]
        public string FullPath { get; set; }

        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "ByRootFolderPath")]
        [Alias("path")]
        public string RootFolderPath { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter IgnoreEpisodesWithFiles { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter IgnoreEpisodesWithoutFiles { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter SearchForMissingEpisodes { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter PassThru { get; set; }

        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [Alias("Title")]
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
        public SeasonCollection Seasons { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter NotMonitored { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter UseSeasonFolders { get; set; }

        [Parameter(Mandatory = false, ValueFromPipelineByPropertyName = true)]
        public int TVRageId { get; set; }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            bool usf = false;
            if (this.MyInvocation.BoundParameters.ContainsKey("UseSeasonFolders"))
            {
                usf = this.UseSeasonFolders.ToBool();
            }
            else if (this.Seasons != null && this.Seasons.Count > 1)
            {
                usf = true;
            }

            var dict = new Dictionary<string, object>
            {
                { "addOptions", new Dictionary<string, bool>(3)
                    {
                        { "ignoreEpisodesWithFiles", this.IgnoreEpisodesWithFiles.ToBool() },
                        { "ignoreEpisodesWithoutFiles", this.IgnoreEpisodesWithoutFiles.ToBool() },
                        { "searchForMissingEpisodes", this.SearchForMissingEpisodes.ToBool() }
                    }
                },
                { "images", this.Images },
                { "monitored", !this.NotMonitored.ToBool() },
                { "qualityProfileId", 4 },
                { "seasonFolder", usf },
                { "seasons", this.Seasons },
                { "title", this.Name },
                { "titleSlug", this.TitleSlug },
                { "tvdbId", this.TVDBId }
            };

            if (this.MyInvocation.BoundParameters.ContainsKey("TVRageId"))
            {
                dict.Add("tvRageId", this.TVRageId);
            }

            if (!string.IsNullOrEmpty(this.RootFolderPath))
            {
                dict.Add("rootFolderPath", this.RootFolderPath);
            }
            else
            {
                dict.Add("path", this.FullPath);
            }

            string postJson = JsonConvert.SerializeObject(dict, new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore
            });

            base.WriteDebug(postJson);

            if (base.ShouldProcess(string.Format("New Series - {0}", this.Name, "Adding")))
            {
                try
                {
                    string output = _api.SonarrPost("/series", postJson);
                    if (this.PassThru.ToBool())
                    {
                        base.WriteObject(SonarrHttpClient.ConvertToSeriesResult(output));
                    }
                }
                catch (Exception e)
                {
                    base.WriteError(e, ErrorCategory.InvalidResult, postJson);
                }
            }
        }

        #endregion

        #region BACKEND METHODS


        #endregion
    }
}