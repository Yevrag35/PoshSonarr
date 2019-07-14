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
    [Cmdlet(VerbsCommon.Add, "Series", ConfirmImpact = ConfirmImpact.Low)]
    [OutputType(typeof(SeriesResult))]
    [CmdletBinding(PositionalBinding = false)]
    public class AddSeries : BaseSonarrCmdlet
    {
        #region FIELDS/CONSTANTS


        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = true, ValueFromPipeline = true, Position = 0)]
        public SeriesResult Series { get; set; }

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

        }

        #endregion

        #region BACKEND METHODS


        #endregion
    }
}