using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using System.Reflection;

namespace MG.Sonarr.Cmdlets
{
    public abstract class BaseSonarrCmdlet : PSCmdlet
    {
        #region FIELDS/CONSTANTS
        protected private ApiCaller _api;
        protected private bool _noPre;

        #endregion

        #region PARAMETERS


        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing()
        {
            if (!Context.IsConnected)
                throw new SonarrContextNotSetException();

            else
            {
                _api = Context.ApiCaller;
                _noPre = Context.NoApiPrefix;
            }
        }

        #endregion

        #region BACKEND METHODS
        protected private string TryGetSonarrResult(string endpoint)
        {
            try
            {
                string strRes = _api.SonarrGet(endpoint);
                return strRes;
            }
            catch (Exception e)
            {
                base.WriteError(new ErrorRecord(e, e.GetType().FullName, ErrorCategory.ReadError, endpoint));
                return null;
            }
        }

        #endregion
    }
}