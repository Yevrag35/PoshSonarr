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
    /// <summary>
    /// A base PowerShell <see cref="PSCmdlet"/> class that provides functionality for posting commands to Sonarr.
    /// </summary>
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
        /// <summary>
        /// Takes in a generic dictionary of parameters and issues a command to Sonarr with the dictionary as its payload.
        /// </summary>
        /// <param name="parameterDict">The set of parameters to build the POST body with,</param>
        protected void ProcessRequest(IDictionary parameterDict)
        {
            string cmdName = parameterDict["name"] as string;
            string verbMsg = string.Format("Issuing command - {0} at {1}", cmdName, BASE_EP);

            string postBody = JsonConvert.SerializeObject(parameterDict, Formatting.Indented);
            string cmdOut = base.TryPostSonarrResult(BASE_EP, postBody);
            if (!string.IsNullOrEmpty(cmdOut))
            {
                CommandOutput cmdOutput = SonarrHttp.ConvertToSonarrResult<CommandOutput>(cmdOut);
                base.WriteObject(cmdOutput);
            }
        }

        #endregion
    }
}