using MG.Sonarr.Functionality;
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

        private bool _iewf = true;
        private bool _iewof = true;
        private bool _nm;
        private bool _passThru;
        private bool _sfme;
        private bool _usf;

        private SeriesPost newPost;

        #endregion

        #region PARAMETERS

        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true)]
        public SearchSeries Series { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "ByFullPath")]
        public string FullPath { get; set; }

        [Parameter(Mandatory = true, Position = 1, ParameterSetName = "ByRootFolderPath")]
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

        [Parameter(Mandatory = true)]
        [ValidateRange(1, int.MaxValue)]
        public int QualityProfileId { get; set; }

        [Parameter(Mandatory = false)]
        public int[] Tags { get; set; } = new int[] { };

        [Parameter(Mandatory = false)]
        public SeriesType Type { get; set; }

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

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            if ( ! base.HasParameterSpecified(this, x => x.UseSeasonFolders) && this.Series.Seasons.Count > 1)
                _usf = true;

            newPost = this.NewPost(this.Series);
            newPost.IsMonitored = !_nm;
            newPost.Options.IgnoreEpisodesWithFiles = _iewf;
            newPost.Options.IgnoreEpisodesWithoutFiles = _iewof;
            newPost.Options.SearchForMissingEpisodes = _sfme;
            newPost.QualityProfileId = this.QualityProfileId;
            newPost.SeriesType = this.Type;
            newPost.Tags = new HashSet<int>(this.Tags);
            newPost.UsingSeasonFolders = _usf;

            // Add Path
            this.AddPath();

            base.WriteDebug(newPost.ToJson());
            if (base.FormatShouldProcess("Add", "Series: {0}", newPost.Name))
            {
                SeriesResult postBack = base.SendSonarrPost<SeriesResult>(this.Series.GetEndpoint(), newPost);
                if (_passThru)
                    base.SendToPipeline(postBack);
            }
        }

        #endregion

        #region BACKEND METHODS

        private SeriesPost NewPost(SearchSeries result) => SeriesPost.NewPost(result);

        private void AddPath()
        {
            if (base.HasParameterSpecified(this, x => x.RootFolderPath))
                newPost.Path = this.RootFolderPath;
            
            else
            {
                newPost.IsFullPath = true;
                newPost.Path = this.FullPath;
            }
        }

        #endregion
    }
}