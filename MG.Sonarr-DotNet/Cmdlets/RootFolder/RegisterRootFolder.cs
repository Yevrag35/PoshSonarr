using MG.Sonarr.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsLifecycle.Register, "RootFolder", ConfirmImpact = ConfirmImpact.None, SupportsShouldProcess = true)]
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
            if (base.HasParameterSpecified(this, x => x.Path))
                _paths = new List<string>(this.Path);

            else
                _paths = new List<string>();
        }

        protected override void ProcessRecord()
        {
            if (base.HasParameterSpecified(this, x => x.InputObject))
            {
                _paths.Add(this.InputObject);
            }
        }

        protected override void EndProcessing()
        {
            for (int i = 0; i < _paths.Count; i++)
            {
                string path = _paths[i];
                var bodyParams = new SonarrBodyParameters
                {
                    { "path", path }
                };
                if (base.ShouldProcess(path, "Register"))
                    base.SendToPipeline(base.SendSonarrPost<RootFolder>(EP, bodyParams));
            }
        }

        #endregion
    }
}