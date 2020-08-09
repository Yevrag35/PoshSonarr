using MG.Posh.Extensions.Bound;
using MG.Sonarr.Functionality;
using MG.Sonarr.Functionality.Collections;
using MG.Sonarr.Results;
using System;
using System.Management.Automation;

namespace MG.Sonarr.Cmdlets
{
    /// <summary>
    /// A base PowerShell <see cref="PSCmdlet"/> class that provides functionality for posting commands to Sonarr.
    /// </summary>
    [CmdletBinding(SupportsShouldProcess = true)]
    public abstract class BasePostCommandCmdlet : BaseSonarrCmdlet
    {
        #region FIELDS/CONSTANTS
        //protected private const string BASE_EP = "/command";
        protected private SonarrBodyParameters parameters;

        //protected abstract string Command { get; }
        protected abstract Endpoint Command { get; }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            parameters = new SonarrBodyParameters
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
        protected private void ProcessRequest(SonarrBodyParameters parameterDict)
        {
            object cmdName = parameterDict["name"];
            
            if (this.ContainsBuiltinParameter(BuiltInParameter.Verbose))
            {
                string verbMsg = string.Format("Issuing command - to {0}", this.Command.AsString());
                base.WriteVerbose(verbMsg);
            }

            CommandOutput output = base.SendSonarrPost<CommandOutput>(this.Command, parameterDict);
            if (output != null)
            {
                History.Jobs.AddResult(output);
                base.WriteFormatVerbose("Added Job to History: {0}", output.Id);

                base.WriteObject(output);
            }
        }

        #endregion
    }
}