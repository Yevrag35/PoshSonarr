using MG.Sonarr.Results;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsCommon.Search, "Directory", ConfirmImpact = ConfirmImpact.None)]
    [OutputType(typeof(SonarrDirectory))]
    [CmdletBinding()]
    public class SearchDirectory : BaseSonarrCmdlet
    {
        #region FIELDS/CONSTANTS
        private const string EP = "/filesystem?path={0}";

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true)]
        [Alias("FullName")]
        public string Path { get; set; }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            if (!this.Path.EndsWith(@"\"))
                this.Path = this.Path + @"\";

            string fullEp = string.Format(EP, this.Path);

            FileSystem fs = base.SendSonarrGet<FileSystem>(fullEp);
            if (fs != null && fs.Directories != null && fs.Directories.Length > 0)
                base.WriteObject(fs.Directories, true);
        }

        #endregion

        #region BACKEND METHODS


        #endregion
    }
}