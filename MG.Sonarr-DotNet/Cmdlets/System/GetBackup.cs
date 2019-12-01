using MG.Sonarr.Functionality;
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
    [Cmdlet(VerbsCommon.Get, "Backup", ConfirmImpact = ConfirmImpact.None)]
    [OutputType(typeof(SonarrBackupResult))]
    [CmdletBinding(PositionalBinding = false)]
    public class GetBackup : BaseSonarrCmdlet
    {
        #region FIELDS/CONSTANTS


        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = false, Position = 0)]
        public BackupType[] Type { get; set; }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            string jsonStr = base.TryGetSonarrResult("/system/backup");
            if (!string.IsNullOrWhiteSpace(jsonStr))
            {
                List<SonarrBackupResult> backups = SonarrHttp.ConvertToSonarrResults<SonarrBackupResult>(jsonStr, out bool iso);
                
                if (this.Type != null && this.Type.Length > 0)
                {
                    base.WriteObject(backups.Where(x => this.Type.Contains(x.BackupType)), true);
                }
                else
                {
                    base.WriteObject(backups, true);
                }
            }
        }

        #endregion

        #region BACKEND METHODS


        #endregion
    }
}