using MG.Sonarr.Functionality;
using MG.Sonarr.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace MG.Sonarr.Cmdlets.Logging
{
    [Cmdlet(VerbsCommon.Clear, "Log", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Low)]
    [OutputType(typeof(CommandResult))]
    [CmdletBinding]
    public class ClearLog : BasePostCommandCmdlet
    {

        #region FIELDS/CONSTANTS
        private const string CMD_STR = "clearlog";
        private const string SP_MSG = "Issue a clear log command";
        protected override string Command => CMD_STR;

        #endregion

        #region PARAMETERS


        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            if (base.ShouldProcess(SP_MSG, "Clear"))
                base.ProcessRequest(base.parameters);
        }

        #endregion

        #region BACKEND METHODS


        #endregion
    }
}