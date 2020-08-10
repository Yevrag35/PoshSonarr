using MG.Sonarr.Functionality;
using System;
using System.Management.Automation;

namespace MG.Sonarr.Cmdlets
{
    public abstract class BaseEndpointCmdlet : BaseSonarrCmdlet
    {
        #region FIELDS/CONSTANTS
        protected abstract Endpoint Endpoint { get; }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing() => base.BeginProcessing();

        #endregion
    }
}