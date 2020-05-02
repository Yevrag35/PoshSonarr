using MG.Dynamic;
using MG.Sonarr.Functionality;
using MG.Sonarr.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsLifecycle.Enable, "Quality", ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true)]
    [OutputType(typeof(QualityProfile))]
    [CmdletBinding(PositionalBinding = false)]
    public class SetAllowedQuality : BaseSonarrCmdlet, IDynamicParameters
    {
        #region FIELDS/CONSTANTS
        private DynamicLibrary _dynLib;

        private const string QUALITY = "Name";

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = true, ValueFromPipeline = true)]
        [Alias("Profile")]
        public QualityProfile InputObject { get; set; }

        #endregion

        #region DYNAMIC
        public object GetDynamicParameters()
        {
            if (Context.IsConnected && _dynLib == null)
            {
                _dynLib = new DynamicLibrary
                {
                    new DynamicParameter<Quality>(QUALITY, true, Context.AllQualities, x => x.Name, "Quality")
                    {
                        Mandatory = true,
                        Position = 0
                    }
                };
            }
            return _dynLib;
        }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            string[] names = _dynLib.GetParameterValues<string>(QUALITY);
            if (base.FormatShouldProcess(string.Format("Enabling '{0}'", string.Join("', '", names)), "Profile Id: {0}",
                this.InputObject.Id))
            {
                IEnumerable<Quality> _chosen = _dynLib.GetUnderlyingValues<Quality>(QUALITY);
                this.InputObject.ApplyAllowables(_chosen);
                base.WriteObject(this.InputObject);
            }
        }

        #endregion

        #region BACKEND METHODS


        #endregion
    }
}