using MG.Sonarr.Results;
using System;
using System.Management.Automation;

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