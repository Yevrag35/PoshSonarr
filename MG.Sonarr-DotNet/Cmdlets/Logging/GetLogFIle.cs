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
            string jsonRes = base.TryGetSonarrResult(EP);
            if (!string.IsNullOrEmpty(jsonRes))
            {
                List<LogFile> logFiles = SonarrHttp.ConvertToSonarrResults<LogFile>(jsonRes);
                logFiles.Sort(ClassFactory.GenerateLogFileComparer());
                if (this.MyInvocation.BoundParameters.ContainsKey("LogFileId"))
                {
                    base.WriteObject(logFiles.FindAll(x => this.LogFileId.Contains(x.LogFileId)), true);
                }
                else if (this.MyInvocation.BoundParameters.ContainsKey("Name"))
                {
                    IEqualityComparer<string> ig = ClassFactory.NewIgnoreCase();
                    List<LogFile> list = logFiles.FindAll(x => this.Name.Contains(x.FileName, ig));
                    base.WriteObject(list, true);
                }
                else
                    base.WriteObject(logFiles, true);
            }
        }

        #endregion

        #region BACKEND METHODS

        #endregion
    }
}