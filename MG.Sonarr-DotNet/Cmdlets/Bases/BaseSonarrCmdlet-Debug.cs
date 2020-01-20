using MG.Api.Json;
using MG.Api.Json.Extensions;
using MG.Api.Rest;
using MG.Api.Rest.Generic;
using MG.Sonarr.Functionality;
using MG.Sonarr.Results;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Management.Automation;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace MG.Sonarr.Cmdlets
{
    public abstract partial class BaseSonarrCmdlet
    {
        #region API DEBUG METHODS
        /// <summary>
        /// Displays the raw JSON response received from the endpoint in the Debug and Verbose output streams.
        /// </summary>
        /// <param name="jsonResult">The JSON string from the response payload.</param>
        /// <param name="code">The status code from the <see cref="HttpResponseMessage"/>.</param>
        /// <param name="showAllDebug">Indicates whether to show the entire JSON response or to only show the status code.</param>
        protected void WriteApiDebug(string jsonResult, HttpStatusCode code, bool showAllDebug)
        {
            if (this.MyInvocation.BoundParameters.ContainsKey("Debug"))
            {
                string debugJson = null;
                if (showAllDebug && !string.IsNullOrWhiteSpace(jsonResult))
                {
                    JToken tok = JToken.Parse(jsonResult);
                    if (tok != null)
                    {
                        debugJson = tok.ToString();
                    }
                }
                base.WriteVerbose(string.Format("Received response: {0} ({1}", (int)code, code.ToString()));
                base.WriteDebug(string.Format(DEBUG_API_RESPONSE_MSG, (int)code, code.ToString(), Environment.NewLine, debugJson));
            }
        }
        /// <summary>
        /// Displays a non-bodied API-specific debug message if the DebugPreference is set to show Debug-level messages.
        /// It returns the "to-be-used" API uri string no matter what.
        /// </summary>
        /// <param name="endpoint">The endpoint Uri string that the <see cref="SonarrRestClient"/> will execute on.</param>
        /// <param name="method">The method that will be used in the API call.</param>
        /// <param name="apiPath">The parsed API uri to be executed on.</param>
        protected virtual void WriteApiDebug(string endpoint, HttpMethod method, out string apiPath)
        {
            apiPath = Context.SonarrUrl.Path + endpoint;

            string msg = string.Format(
                DEBUG_API_MSG,
                method.Method,
                Context.SonarrUrl.BaseUrl,
                apiPath
            );

            base.WriteDebug(msg);
        }

        /// <summary>
        /// Displays a bodied API-specific debug message if the DebugPreference is set to show Debug-level messages.
        /// It returns the "to-be-used" API uri string no matter what.
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="method"></param>
        /// <param name="body"></param>
        /// <param name="apiPath"></param>
        protected virtual void WriteApiDebug(string endpoint, HttpMethod method, string body, out string apiPath)
        {
            apiPath = Context.SonarrUrl.Path + endpoint;

            string msg = string.Format(
                DEBUG_API_AND_BODY_MSG,
                method.Method,
                Context.SonarrUrl.BaseUrl,
                apiPath,
                Environment.NewLine,
                body
            );

            base.WriteDebug(msg);
        }

        #endregion

        #region FORMAT DEBUG/VERBOSE METHODS
        protected void WriteFormatDebug(string format, params object[] arguments)
        {
            base.WriteDebug(string.Format(format, arguments));
        }

        protected void WriteFormatVerbose(string format, params object[] arguments)
        {
            base.WriteVerbose(string.Format(format, arguments));
        }

        #endregion
    }
}
