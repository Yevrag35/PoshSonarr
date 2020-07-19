using MG.Posh.Extensions.Writes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Management.Automation;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace MG.Sonarr.Cmdlets
{
    /// <summary>
    /// The main base <see cref="PSCmdlet"/> class for all PoshSonarr cmdlets.  Includes custom API methods along with advanced error-handling.
    /// </summary>
    public abstract partial class BaseSonarrCmdlet : PSCmdlet
    {
        #region FIELDS/CONSTANTS
        private const string CONNECT_EP = "/system/status";
        [Obsolete]
        private const string CONTENT_TYPE = "application/json";
        private const string DEBUG_API_MSG = "Sending {0} request to: {1}{2}";
        private const string DEBUG_API_AND_BODY_MSG = DEBUG_API_MSG + "{3}{3}REQUEST BODY:{4}";
        private const string DEBUG_API_RESPONSE_MSG = "RESPONSE ({0} {1}): {2}{2}{3}";

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing()
        {
            if (!Context.IsConnected)
            {
                var exc = new SonarrContextNotSetException();
                var errRec = new ErrorRecord(
                    exc,
                    exc.GetType().FullName,
                    ErrorCategory.ConnectionError,
                    null
                );
                errRec.CategoryInfo.Reason = "Context not set.  \"Connect-Instance\" has not been executed.";
                errRec.ErrorDetails = new ErrorDetails("Context not set.  \"Connect-SonarrInstance\" has not been executed.")
                {
                    RecommendedAction = "Run \"Connect-SonarrInstance\" before any other cmdlets."
                };
                base.ThrowTerminatingError(errRec);
            }
        }

        #endregion

        #region FILTERING METHODS
        protected internal IEnumerable<T> FilterWhere<T>(IEnumerable<T> items, params Expression<Func<T, bool>>[] parameterExpressions)
        {
            if (parameterExpressions == null || parameterExpressions.Length <= 0)
                return items;

            for (int i = 0; i < parameterExpressions.Length; i++)
            {
                var pExp = parameterExpressions[i].Compile();
                items = items.Where(pExp);
            }
            return items;
        }

        #endregion

        #region PIPELINE METHOD
        /// <summary>
        /// Sends an <see cref="object"/> to the PowerShell console and optionally specifies whether or not to enumerate it
        /// if it's a collection.
        /// </summary>
        /// <param name="obj">The object to send to the pipeline</param>
        /// <param name="enumerateCollection">Indicates whether the object will be enumerated as its sent.</param>
        protected void SendToPipeline(object obj, bool enumerateCollection = true)
        {
            if (obj != null)
            {
                base.WriteObject(obj, enumerateCollection);
            }
        }

        #endregion

        #region SHOULD PROCESS METHODS
        protected bool FormatShouldProcess(string action, string stringFormat, params object[] arguments)
        {
            string msg = string.Format(stringFormat, arguments);
            return base.ShouldProcess(msg, action);
        }

        #endregion

        #region PSOBJECT METHODS
        

        #endregion
    }
}