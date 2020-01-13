using MG.Sonarr.Functionality;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using System.Reflection;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "Log", ConfirmImpact = ConfirmImpact.None)]
    [CmdletBinding(PositionalBinding = false)]
    public class GetLog : LazySonarrCmdlet
    {
        #region FIELDS/CONSTANTS
        protected override string Endpoint => "/log";
        private string _sortKey;

        #endregion

        #region PARAMETERS

        [Parameter(Mandatory = false, Position = 1)]
        public LogLevel Severity { get; set; }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            if (base.HasParameterSpecified(this, x => x.Severity))
            {
                _list.Add("filterKey=level");
                _list.Add(string.Format("filterValue={0}", this.Severity.ToString()));
            }
            base.ProcessRecord();
        }

        #endregion

        #region BACKEND METHODS


        #endregion
    }
}