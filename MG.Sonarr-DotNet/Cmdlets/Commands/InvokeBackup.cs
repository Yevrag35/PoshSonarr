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
    /// <summary>
    /// <para type="synopsis">Instructs Sonarr to perform a backup.</para>
    /// <para type="description">Sonarr performs a backup of its database when this cmdlet is run.</para>
    /// </summary>
    [Cmdlet(VerbsLifecycle.Invoke, "Backup", ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true)]
    [OutputType(typeof(CommandOutput))]
    [Alias("Backup-")]
    [CmdletBinding(PositionalBinding = false)]
    public class InvokeBackup : BasePostCommandCmdlet
    {
        #region FIELDS/CONSTANTS
        protected override string Command => "Backup";

        #endregion

        #region PARAMETERS

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            string msg = "Issue new backup request";

            if (base.ShouldProcess(msg, "Backup"))
            {
                base.ProcessRequest(parameters);
            }
        }

        #endregion
    }
}