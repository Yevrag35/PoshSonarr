using MG.Dynamic;
using MG.Sonarr.Results;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsCommon.New, "QualityProfile", ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true)]
    [OutputType(typeof(QualityDefinition))]
    [CmdletBinding(PositionalBinding = false)]
    public class NewQualityProfile : BaseSonarrCmdlet, IDynamicParameters
    {
        #region FIELDS/CONSTANTS
        private DynamicLibrary _dynLib;
        private bool _upAllowed;

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = true, Position = 0)]
        public string Name { get; set; }

        #endregion

        #region DYNAMIC
        public object GetDynamicParameters()
        {
            if (_dynLib == null)
            {
                _dynLib = new DynamicLibrary
                {
                    new DynamicParameter<QualityDefinition>("Quality", Context.Qualities, x => x.Title, "Title", true)
                    {
                        Mandatory = true,
                        Position = 1
                    },
                    new DynamicParameter<QualityDefinition>("Cutoff", Context.Qualities, x => x.Title, "Title", false)
                    {
                        Mandatory = true,
                        Position = 2
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
            var qualities = _dynLib.GetParameterValues<string>("Quality").ToList();
            string cutoff = _dynLib.GetParameterValue<string>("Cutoff");
            var ieq = new IgnoreCase();
            if (!qualities.Contains(cutoff, ieq))
                qualities.Add(cutoff);


        }

        #endregion

        #region BACKEND METHODS

        
        private class IgnoreCase : IEqualityComparer<string>
        {
            public bool Equals(string x, string y) => x.Equals(y, StringComparison.CurrentCultureIgnoreCase);
            public int GetHashCode(string s) => s.GetHashCode();
        }

        #endregion
    }
}