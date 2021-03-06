﻿using MG.Sonarr.Results;
using System;
using System.Management.Automation;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsCommon.Rename, "Tag", ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true)]
    [OutputType(typeof(Tag))]
    [CmdletBinding(PositionalBinding = false)]
    public class RenameTag : TagCmdlet
    {
        #region FIELDS/CONSTANTS
        private bool _passThru;

        private const string WHAT_IF_ACT = "Rename to '{0}'";
        private const string WHAT_IF_MSG = "Tag Id: {0}";

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true)]
        [Alias("TagId")]
        public int Id { get; set; }

        [Parameter(Mandatory = true, Position = 1, ValueFromPipelineByPropertyName = true)]
        [Alias("NewName")]
        public string NewLabel { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter PassThru
        {
            get => _passThru;
            set => _passThru = value;
        }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            if (base.FormatShouldProcess(string.Format(WHAT_IF_ACT, this.NewLabel), WHAT_IF_MSG, this.Id))
            {
                Tag modified = base.RenameTag(this.Id, this.NewLabel);
                if (_passThru)
                    base.SendToPipeline(modified);
            }
        }

        #endregion
    }
}