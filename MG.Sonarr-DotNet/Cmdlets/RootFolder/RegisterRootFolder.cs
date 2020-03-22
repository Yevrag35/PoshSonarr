using MG.Posh.Extensions.Bound;
using MG.Sonarr.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsLifecycle.Register, "RootFolder", ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true)]
    [OutputType(typeof(RootFolder))]
    [Alias("New-RootFolder")]
    [CmdletBinding(PositionalBinding = false, DefaultParameterSetName = "ViaPipeline")]
    public class RegisterRootFolder : BaseSonarrCmdlet
    {
        #region FIELDS/CONSTANTS
        private const string EP = "/rootfolder";
        private List<string> _paths;

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = true, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, 
            ParameterSetName = "ViaPipeline", DontShow = true)]
        [Alias("FullName")]
        public string InputObject { get; set; }

        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "ViaSpecifiedPaths")]
        public string[] Path { get; set; }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            if (this.ContainsParameter(x => x.Path))
                _paths = new List<string>(this.Path);

            else
                _paths = new List<string>();
        }

        protected override void ProcessRecord()
        {
            if (this.ContainsParameter(x => x.InputObject))
            {
                _paths.Add(this.InputObject);
            }
        }

        protected override void EndProcessing()
        {
            for (int i = 0; i < _paths.Count; i++)
            {
                string path = _paths[i];
                if (base.FormatShouldProcess("Register", "Path: {0}", path))
                {
                    base.SendToPipeline(base.SendSonarrPost<RootFolder>(EP, new RootFolderPost(path)));
                }
            }
        }

        #endregion
    }
}