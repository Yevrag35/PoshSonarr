using MG.Sonarr.Functionality;
using MG.Sonarr.Results;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsCommon.Remove, "Tag", ConfirmImpact = ConfirmImpact.High, SupportsShouldProcess = true, 
        DefaultParameterSetName = "None")]
    [CmdletBinding(PositionalBinding = false)]
    public class RemoveTag : BaseSonarrCmdlet
    {
        #region FIELDS/CONSTANTS
        private const string EP_WITH_ID = "/tag/{0}";
        private bool _force;
        private bool _passThru;

        private bool _clearAll;
        private string _ep;
        private int[] _ids;

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = true, ParameterSetName = "RemoveFromObjectWithAll")]
        public SwitchParameter ClearAll
        {
            get => _clearAll;
            set => _clearAll = value;
        }

        [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true, ParameterSetName = "None")]
        [Parameter(Mandatory = true, ParameterSetName = "RemoveFromObjectWithIds")]
        public int[] Id
        {
            get => null;
            set => _ids = value;
        }

        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = "RemoveFromObjectWithIds")]
        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = "RemoveFromObjectWithAll")]
        public ISupportsTagUpdate RemoveFrom { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter Force
        {
            get => _force;
            set => _force = value;
        }

        [Parameter(Mandatory = false, ParameterSetName = "RemoveFromObjectWithIds")]
        [Parameter(Mandatory = false, ParameterSetName = "RemoveFromObjectWithAll")]
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
            string target = this.GetMessage(_ids);

            if (_force || base.ShouldProcess(target, "Remove"))
            {
                if (base.HasParameterSpecified(this, x => x.RemoveFrom))
                {
                    _ep = this.RemoveFrom.GetEndpoint();
                    this.RemoveFrom.RemoveTags(_ids);
                    object modified = base.SendSonarrPut(_ep, this.RemoveFrom, this.RemoveFrom.GetType());
                    if (_passThru)
                        base.SendToPipeline(modified);
                }
                else
                {
                    foreach (int oneId in _ids)
                    {
                        _ep = string.Format(EP_WITH_ID, oneId);
                        base.SendSonarrDelete(_ep);
                    }
                }
            }
        }

        #endregion

        private string GetMessage(int[] ids)
        {
            if (_ids == null)
                return "All Tags";

            else
                return string.Format("Tag - {0}", string.Join(", ", ids));
        }
    }
}