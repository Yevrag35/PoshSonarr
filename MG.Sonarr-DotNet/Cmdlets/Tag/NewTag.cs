using MG.Sonarr.Results;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using System.Reflection;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsCommon.New, "Tag", ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true)]
    [OutputType(typeof(Tag))]
    [CmdletBinding(PositionalBinding = false)]
    public class NewTag : TagCmdlet
    {
        #region FIELDS/CONSTANTS
        private const string WHAT_IF_MSG = "Tag - {0}";

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = true, Position = 0)]
        public string Label { get; set; }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            if (base.FormatShouldProcess("New", WHAT_IF_MSG, this.Label))
            {
                base.SendToPipeline(base.NewTag(this.Label));
            }
        }

        #endregion
    }
}