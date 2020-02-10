using MG.Sonarr.Functionality;
using MG.Sonarr.Results;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Net.Http;
using System.Reflection;
using System.Security;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsCommon.Remove, "Series", ConfirmImpact = ConfirmImpact.High, SupportsShouldProcess = true, DefaultParameterSetName = "ViaPipeline")]
    [CmdletBinding(PositionalBinding = false)]
    public class RemoveSeries : BaseSonarrCmdlet
    {
        #region FIELDS/CONSTANTS
        private bool _deleteFiles;
        private bool _force;

        #endregion

        #region PARAMETERS

        [Parameter(Mandatory = true, ValueFromPipeline = true, DontShow = true, ParameterSetName = "ViaPipeline")]
        public SeriesResult InputObject { get; set; }

        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "ByExplicitSeriesId")]
        [Alias("Id")]
        public int SeriesId { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter DeleteFiles
        {
            get => _deleteFiles;
            set => _deleteFiles = value;
        }

        [Parameter(Mandatory = false)]
        public SwitchParameter Force
        {
            get => _force;
            set => _force = value;
        }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            if (base.HasParameterSpecified(this, x => x.InputObject))
                this.SeriesId = this.InputObject.Id;

            string apiUri = string.Format("/series/{0}?deleteFiles={1}", this.SeriesId, _deleteFiles.ToString());

            if (_force || base.FormatShouldProcess("Delete", "Series Id: {0}", this.SeriesId))
                base.SendSonarrDelete(apiUri);
            
        }

        #endregion
    }
}