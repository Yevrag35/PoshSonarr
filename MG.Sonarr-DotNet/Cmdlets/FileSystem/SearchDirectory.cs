using MG.Sonarr.Functionality;
using MG.Sonarr.Functionality.Url;
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
        private Endpoint Endpoint { get; } = Endpoint.FileSystem;
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

            Endpoint ep = this.Endpoint.WithQuery(new FileSystemParameter(this.Path, !_excludeFiles));

            

            FileSystem fs = base.SendSonarrGet<FileSystem>(ep);
            if (fs != null)
            {
                List<FileSystemEntry> list = fs.ToAllList();
                base.SendToPipeline(list);
            }
        }

        #endregion
    }
}