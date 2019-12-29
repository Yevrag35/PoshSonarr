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
            if (HasParameterSpecified(this, x => x.Path))
            {
                return;
            }
        }

        protected override void ProcessRecord()
        {

        }

        //protected override void EndProcessing()
        //{

        //}

        #endregion

        #region BACKEND METHODS


        #endregion
    }
}