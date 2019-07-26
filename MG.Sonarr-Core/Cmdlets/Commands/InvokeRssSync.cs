﻿using MG.Sonarr.Results;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Reflection;
using System.Security;

namespace MG.Sonarr.Cmdlets.Commands
{
    [Cmdlet(VerbsLifecycle.Invoke, "RssSync", ConfirmImpact = ConfirmImpact.None)]
    [CmdletBinding(PositionalBinding = false)]
    [OutputType(typeof(CommandOutput))]
    [Alias("Start-RssSync")]
    public class InvokeRssSync : BasePostCommandCmdlet
    {
        #region FIELDS/CONSTANTS
        protected override string Command => "RssSync";

        #endregion

        #region PARAMETERS


        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            base.ProcessRecord();
        }

        #endregion

        #region METHODS


        #endregion
    }
}