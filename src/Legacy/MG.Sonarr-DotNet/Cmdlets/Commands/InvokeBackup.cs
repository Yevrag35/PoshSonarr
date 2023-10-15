using MG.Sonarr.Results;
using System;
using System.Management.Automation;

namespace MG.Sonarr.Cmdlets
{
#if (NETCOREAPP == false)
    /// <summary>
    /// <para type="synopsis">Instructs Sonarr to perform a backup.</para>
    /// <para type="description">Sonarr performs a backup of its database when this cmdlet is run.</para>
    /// </summary>
#endif
    [Cmdlet(VerbsLifecycle.Invoke, "Backup", ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true)]
    [OutputType(typeof(CommandOutput))]
    [Alias("Backup-")]
    [CmdletBinding(PositionalBinding = false)]
    public class InvokeBackup : BasePostCommandCmdlet
    {
        #region FIELDS/CONSTANTS
        protected sealed override string Command => "Backup";

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