using MG.Sonarr.Functionality;
using MG.Sonarr.Results;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsCommon.New, "QualityProfile")]
    [OutputType(typeof(QualityProfile))]
    [CmdletBinding(PositionalBinding = false)]
    public class NewQualityProfile : BaseSonarrCmdlet
    {
        #region FIELDS/CONSTANTS
        private const string EP = "/profile";

        #endregion

        #region PARAMETERS


        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing()
        {
            base.BeginProcessing();

        }

        protected override void ProcessRecord()
        {

        }

        protected override void EndProcessing()
        {

        }

        #endregion

        #region BACKEND METHODS


        #endregion
    }
}