using MG.Dynamic;
using MG.Posh.Extensions.Bound;
using MG.Sonarr.Functionality;
using MG.Sonarr.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Management.Automation;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsLifecycle.Enable, "Quality", ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true,
        DefaultParameterSetName = "ByName")]
    [OutputType(typeof(QualityProfile))]
    [CmdletBinding(PositionalBinding = false)]
    public class SetAllowedQuality : BaseSonarrCmdlet, IDynamicParameters
    {
        #region FIELDS/CONSTANTS
        private DynamicLibrary _dynLib;

        private const string QUALITY = "Name";
        private const string ID = "Id";

        private bool _passThru;

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = true, ValueFromPipeline = true)]
        [Alias("Profile")]
        public QualityProfile InputObject { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter PassThru
        {
            get => _passThru;
            set => _passThru = value;
        }

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
                        ParameterSetName = "ByName",
                        Position = 0
                    },
                    new DynamicParameter<Quality>(ID, true, Context.AllQualities, x => x.Id)
                    {
                        Mandatory = true,
                        ParameterSetName = "ById"
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
            IEnumerable<object> objs = null;
            Action<QualityProfile, IEnumerable<object>> exp = null;
            if (this.ContainsAnyParameterNames(ID))
            {
                objs = _dynLib.GetParameterValues(ID).ToArray();
                exp = (a, o) => ApplyIds(a, o);
            }
            else if (this.ContainsAnyParameterNames(QUALITY))
            {
                objs = _dynLib.GetParameterValues(QUALITY);
                exp = (a, o) => ApplyNames(a, o);
            }
            if (base.FormatShouldProcess(this.GetMessage(objs), "Profile Id: {0}",
                this.InputObject.Id))
            {
                exp.Invoke(this.InputObject, objs);
                
                if (_passThru)
                    base.WriteObject(this.InputObject);
            }
        }

        private string GetMessage(IEnumerable<object> objs)
        {
            return string.Format("Enabling '{0}'", string.Join("', '", objs));
        }
        private void ApplyIds(QualityProfile qp, IEnumerable<object> ids)
        {
            qp.ApplyAllowablesById(ids.Select(x => Convert.ToInt32(x)).ToArray());
        }
        private void ApplyNames(QualityProfile qp, IEnumerable<object> names)
        {
            qp.ApplyAllowablesByName(names.Cast<string>().ToArray());
        }

        #endregion

        #region BACKEND METHODS


        #endregion
    }
}