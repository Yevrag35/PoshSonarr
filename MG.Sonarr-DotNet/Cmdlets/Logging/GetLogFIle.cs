using MG.Sonarr.Functionality;
using MG.Sonarr.Results;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Management.Automation;

namespace MG.Sonarr.Cmdlets.Logging
{
    [Cmdlet(VerbsCommon.Get, "LogFile", ConfirmImpact = ConfirmImpact.None, DefaultParameterSetName = "ByLogFileId")]
    [OutputType(typeof(LogFile))]
    [CmdletBinding(PositionalBinding = false)]
    public class GetLogFile : BaseSonarrCmdlet
    {
        #region FIELDS/CONSTANTS
        private const string EP = "/log/file";

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = false, Position = 0, ParameterSetName = "ByLogFileId")]
        public int[] LogFileId { get; set; }

        [Parameter(Mandatory = false, Position = 0, ParameterSetName = "ByLogName")]
        public string[] Name { get; set; }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            List<LogFile> logFiles = base.SendSonarrListGet<LogFile>(EP);
            if (logFiles != null && logFiles.Count > 0)
            {
                base.WriteObject(this.FilterResults(logFiles), true);
            }
        }

        #endregion

        #region BACKEND METHODS
        private IEnumerable<LogFile> FilterResults(List<LogFile> logFiles)
        {
            logFiles.Sort();
            if (this.MyInvocation.BoundParameters.ContainsKey("LogFileId"))
            {
                return logFiles.Where(x => this.LogFileId.Contains(x.LogFileId));
            }
            else if (this.MyInvocation.BoundParameters.ContainsKey("Name"))
            {
                IEqualityComparer<string> ig = ClassFactory.NewIgnoreCase();
                return logFiles.Where(x => this.Name.Contains(x.FileName, ig));
            }
            else
                return logFiles;
        }

        #endregion
    }
}