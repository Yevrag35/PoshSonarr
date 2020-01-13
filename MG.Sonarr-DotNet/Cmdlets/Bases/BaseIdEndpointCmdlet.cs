using System;
using System.Management.Automation;

namespace MG.Sonarr.Cmdlets
{
    public abstract class BaseIdEndpointCmdlet : BaseEndpointCmdlet
    {
        #region FIELDS/CONSTANTS
        protected string IdEndpoint => this.Endpoint + "/{0}";

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing() => base.BeginProcessing();

        #endregion

        #region METHODS
        protected string FormatWithId(int id) => string.Format(this.IdEndpoint, id);
        protected string FormatWithId(long id) => string.Format(this.IdEndpoint, id);

        #endregion
    }
}