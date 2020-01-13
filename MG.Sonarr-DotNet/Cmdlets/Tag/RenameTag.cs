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
    [Cmdlet(VerbsCommon.Rename, "Tag", ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true)]
    [OutputType(typeof(Tag))]
    [CmdletBinding(PositionalBinding = false)]
    public class RenameTag : BaseSonarrCmdlet
    {
        #region FIELDS/CONSTANTS
        private const string EP = "/tag";
        private bool _passThru;

        private const string PARAM_ID = "id";
        private const string PARAM_LBL = "label";

        private const string WHAT_IF_ACT = "Rename to '{0}'";
        private const string WHAT_IF_MSG = "Tag Id: {0}";

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true)]
        [Alias("TagId")]
        public int Id { get; set; }

        [Parameter(Mandatory = true, Position = 1)]
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
                SonarrBodyParameters postBody = this.GetPostParameters(this.Id, this.NewLabel);
                Tag setTag = base.SendSonarrPut<Tag>(EP, postBody);
                if (_passThru)
                    base.SendToPipeline(setTag);
            }
        }

        #endregion

        #region METHODS
        private SonarrBodyParameters GetPostParameters(int id, string label)
        {
            return new SonarrBodyParameters
            {
                { PARAM_ID, id },
                { PARAM_LBL, label }
            };
        }

        #endregion
    }
}