using MG.Posh.Extensions.Bound;
using MG.Sonarr.Functionality;
using MG.Sonarr.Results;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;

namespace MG.Sonarr.Cmdlets.Commands
{
    [Cmdlet(VerbsCommon.Get, "CommandHistory")]
    [Alias("Get-JobHistory")]
    [OutputType(typeof(IPastJob))]
    public class GetCommandHistory : BaseSonarrCmdlet, IDynamicParameters
    {
        #region FIELDS/CONSTANTS
        private RuntimeDefinedParameterDictionary _lib;
        private const string PARAM = "Id";

        #endregion

        #region PARAMETERS


        #endregion

        #region DYNAMIC
        public object GetDynamicParameters()
        {
            _lib = new RuntimeDefinedParameterDictionary();
            RuntimeDefinedParameter rtParam = new RuntimeDefinedParameter(PARAM, typeof(long[]), new Collection<Attribute>
            {
                new ParameterAttribute
                {
                    Mandatory = false,
                    Position = 0
                }
            });
            if (History.IsInitialized() && History.Jobs.Count > 0)
            {
                var set = new ValidateSetAttribute(History.Jobs.Select(x => Convert.ToString(x.Id)).ToArray());
                rtParam.Attributes.Add(set);
            }

            _lib.Add(PARAM, rtParam);
            return _lib;
        }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            IEnumerable<IPastJob> jobs = null;
            if (this.MyInvocation.BoundParameters.ContainsKey(PARAM))
            {
                long[] chosen = _lib[PARAM].Value as long[];
                jobs = History.Jobs.FindById(chosen);
            }
            else
            {
                jobs = History.Jobs;
            }
            base.SendToPipeline(jobs);
        }

        #endregion

        #region BACKEND METHODS


        #endregion
    }
}