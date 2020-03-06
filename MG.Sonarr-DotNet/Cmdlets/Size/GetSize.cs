using MG.Sonarr.Functionality;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "Size", ConfirmImpact = ConfirmImpact.None)]
    [OutputType(typeof(TotalSize))]
    [CmdletBinding(PositionalBinding = false)]
    public class GetSize : PSCmdlet
    {
        #region PARAMETERS
        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true)]
        [Alias("io")]
        public ICanCalculate InputObject { get; set; }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord() => base.WriteObject(TotalSize.Get(this.InputObject));

        #endregion
    }
}