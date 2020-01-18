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
    [Cmdlet(VerbsCommon.Get, "DelayProfile", ConfirmImpact = ConfirmImpact.None)]
    [OutputType(typeof(DelayProfile))]
    [CmdletBinding(PositionalBinding = false)]
    public class GetDelayProfile : DelayProfileCmdlet
    {
        #region FIELDS/CONSTANTS

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = false, Position = 0)]
        public int[] Id { get; set; }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            if (!base.HasParameterSpecified(this, x => x.Id))
                base.SendToPipeline(base.GetAllDelayProfiles());

            else
                base.SendToPipeline(base.GetDelayProfileByIds(this.Id));
        }

        #endregion
    }
}