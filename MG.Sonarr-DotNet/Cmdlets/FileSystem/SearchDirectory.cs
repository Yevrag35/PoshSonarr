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
    [OutputType(typeof(FileSystemEntry))]
    [CmdletBinding(PositionalBinding = false)]
    public class SearchDirectory : BaseSonarrCmdlet
    {
        #region FIELDS/CONSTANTS
        private const string EP = "/filesystem?path={0}";
        private const string EP_WITH_FILES = EP + "&includeFiles=true";

        private bool _excludeFiles;

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true)]
        [Alias("FullName")]
        public string Path { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter ExcludeFiles
        {
            get => _excludeFiles;
            set => _excludeFiles = value;
        }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing()
        {
            base.BeginProcessing();
        }

        protected override void ProcessRecord()
        {
            if (!this.Path.EndsWith(@"\"))
            {
                this.Path = this.Path + @"\";
            }

            string ep = EP_WITH_FILES;
            if (_excludeFiles)
            {
                ep = EP;
            }

            string fullEp = string.Format(ep, this.Path);

            FileSystem fs = base.SendSonarrGet<FileSystem>(fullEp);
            if (fs != null)
            {
                List<FileSystemEntry> list = fs.ToAllList();
                base.SendToPipeline(list);
            }
        }

        #endregion
    }
}