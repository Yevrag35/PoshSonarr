using MG.Sonarr.Results;
using System;
using System.Collections.Generic;
using System.Management.Automation;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "Update", ConfirmImpact = ConfirmImpact.None)]
    [OutputType(typeof(Update))]
    [CmdletBinding(PositionalBinding = false)]
    public class GetUpdate : BaseSonarrCmdlet
    {
        #region FIELDS/CONSTANTS
        private const string EP = "/update";
        private bool _showOlder;

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = false)]
        public SwitchParameter IncludeOlder
        {
            get => _showOlder;
            set => _showOlder = value;
        }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            List<Update> allUpdates = base.SendSonarrListGet<Update>(EP);
            base.SendToPipeline(this.ApplyFilter(allUpdates));
        }

        #endregion

        #region BACKEND METHODS
        private object ApplyFilter(List<Update> updates)
        {
            if (!_showOlder && updates != null && updates.Count > 0)
                return updates.Find(x => x.Latest);

            else
                return updates;
        }

        #endregion
    }
}