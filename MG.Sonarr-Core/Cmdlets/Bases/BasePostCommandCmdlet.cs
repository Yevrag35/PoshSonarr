using MG.Sonarr.Results;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using System.Reflection;

namespace MG.Sonarr.Cmdlets
{
    [CmdletBinding(SupportsShouldProcess = true)]
    public abstract class BasePostCommandCmdlet : BaseSonarrCmdlet
    {
        #region FIELDS/CONSTANTS
        protected private const string BASE_EP = "/command";
        protected private Dictionary<string, object> parameters;

        protected abstract string Command { get; }

        #endregion

        #region PARAMETERS


        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            parameters = new Dictionary<string, object>(2)
            {
                { "name", this.Command }
            };
        }

        protected override void ProcessRecord()
        {
            this.ProcessRequest(parameters);
        }

        #endregion

        #region BACKEND METHODS
        protected void ProcessRequest(IDictionary parameterDict)
        {
            string postBody = JsonConvert.SerializeObject(parameterDict, Formatting.Indented);
            string cmdOut = _api.SonarrPost(BASE_EP, postBody);
            if (!string.IsNullOrEmpty(cmdOut))
            {
                CommandOutput cmdOutput = SonarrHttpClient.ConvertToSonarrResult<CommandOutput>(cmdOut);
                base.WriteObject(cmdOutput);
            }
        }

        #endregion
    }
}