using MG.Dynamic;
using MG.Sonarr.Functionality;
using MG.Sonarr.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace MG.Sonarr.Cmdlets.QualityProfiles
{
    [Cmdlet(VerbsLifecycle.Disable, "Quality", ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true)]
    [OutputType(typeof(QualityProfile))]
    [CmdletBinding(PositionalBinding = false)]
    public class DisableQuality : BaseSonarrCmdlet, IDynamicParameters
    {
        #region FIELDS/CONSTANTS
        private DynamicLibrary _dynLib;

        private const string QUALITY = "Name";
        private IEnumerable<string> _names;
        private IEqualityComparer<string> _comparer;

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
        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            _comparer = ClassFactory.NewIgnoreCase();
        }

        protected override void ProcessRecord()
        {
            _names = _dynLib.GetParameterValues<string>(QUALITY);
            if (_names.Contains(this.InputObject.Cutoff.Name, _comparer))
            {
                base.WriteWarning(string.Format("You cannot disable the cutoff resolution \"{0}\".  Skipping...", this.InputObject.Cutoff.Name));
                _names = _names.Where(x => !x.Equals(this.InputObject.Cutoff.Name, StringComparison.CurrentCultureIgnoreCase));
            }
            if (base.FormatShouldProcess(string.Format("Disabling '{0}'", string.Join("', '", _names)), "Profile Id: {0}",
                this.InputObject.Id))
            {
                IEnumerable<Quality> _chosen = _dynLib.GetUnderlyingValues<Quality>(QUALITY);
                this.InputObject.ApplyDisallowables(_chosen);
                base.WriteObject(this.InputObject);
            }
        }

        #endregion

        #region BACKEND METHODS


        #endregion
    }
}