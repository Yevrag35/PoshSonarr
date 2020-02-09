﻿using MG.Sonarr.Functionality;
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
    [OutputType(typeof(Backup))]
    [CmdletBinding(PositionalBinding = false)]
    public class GetBackup : BaseSonarrCmdlet
    {
        #region FIELDS/CONSTANTS
        private const string EP = "/system/backup";

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = false, Position = 0)]
        public BackupType[] Type { get; set; }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            List<Backup> allBackups = base.SendSonarrListGet<Backup>(EP);
            base.SendToPipeline(this.Filter(allBackups));
        }

        #endregion

        #region BACKEND METHODS
        private List<Backup> Filter(List<Backup> allBackups)
        {
            if (base.HasParameterSpecified(this, x => x.Type))
                return allBackups.FindAll(x => this.Type.Contains(x.Type));

            else
                return allBackups;
        }

        #endregion
    }
}