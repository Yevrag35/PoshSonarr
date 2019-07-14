using MG.Sonarr.Results;
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
        [Parameter(Mandatory = true, ValueFromPipeline = true, Position = 0)]
        [Alias("InputObject")]
        public SeriesResult Series { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "ByFullPath")]
        public string FullPath { get; set; }

        [Parameter(Mandatory = true, Position = 1, ParameterSetName = "ByRootFolderPath")]
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

        //[Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true, Position = 1)]
        //[Alias("Title")]
        //public string Name { get; set; }

        //[Parameter(Mandatory = true, Position = 0)]
        //[Alias("Path")]
        //public string DiskPath { get; set; }

        //[Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true)]
        //public int TVDBId { get; set; }

        //[Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true)]
        //public int QualityProfileId { get; set; }

        //[Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true)]
        //public string TitleSlug { get; set; }

        //[Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true)]
        //public SeriesImage[] Images { get; set; }

        //[Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true)]
        //public Season[] Seasons { get; set; }

        #endregion

        #region DYNAMIC


        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            var parameters = new Dictionary<string, object>()
            {
                { "addOptions", new Dictionary<string, bool>(3)
                    {
                        { "ignoreEpisodesWithFiles", this.IgnoreEpisodesWithFiles.ToBool() },
                        { "ignoreEpisodesWithoutFiles", this.IgnoreEpisodesWithoutFiles.ToBool() },
                        { "searchForMissingEpisodes", this.SearchForMissingEpisodes.ToBool() }
                    }
                }
            };
            if (!string.IsNullOrEmpty(this.RootFolderPath))
            {
                parameters.Add("rootFolderPath", this.RootFolderPath);
            }
            else
            {
                parameters.Add("path", this.FullPath);
            }

            string postJson = this.Series.ToJson(parameters);

            base.WriteDebug(postJson);

            if (base.ShouldProcess(string.Format("New Series - {0}", this.Series.Title, "Adding")))
            {
                string output = _api.SonarrPost("/series", postJson);

                if (this.PassThru.ToBool())
                {
                    base.WriteObject(SonarrHttpClient.ConvertToSeriesResult(output));
                }
            }
        }

        #endregion

        #region BACKEND METHODS


        #endregion
    }
}