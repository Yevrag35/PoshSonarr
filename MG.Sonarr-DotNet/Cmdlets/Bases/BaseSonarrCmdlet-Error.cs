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
        #region EXCEPTION METHODS

        /// <summary>
        /// Takes in an <see cref="Exception"/> and returns the innermost <see cref="Exception"/> as a result.
        /// </summary>
        /// <param name="e">The exception to pull the innermost <see cref="Exception"/> from.</param>
        protected virtual Exception GetAbsoluteException(Exception e)
        {
            while (e.InnerException != null)
            {
                e = e.InnerException;
            }
            return e;
        }

        /// <summary>
        /// Issues a <see cref="PSCmdlet"/>.WriteError from a given string message and <see cref="ErrorCategory"/>.
        /// </summary>
        /// <param name="message">The exception message to be displayed in the <see cref="ErrorRecord"/>.</param>
        /// <param name="category">The category of the error.</param>
        protected void WriteError(string message, ErrorCategory category) =>
            this.WriteError(new ArgumentException(message), category, null);

        /// <summary>
        /// Issues a <see cref="PSCmdlet"/>.WriteError from a given string message, <see cref="ErrorCategory"/>, and Target Object.
        /// </summary>
        /// <param name="message">The exception message to be displayed in the <see cref="ErrorRecord"/>.</param>
        /// <param name="category">The category of the error.</param>
        /// <param name="targetObject">The object used as the 'targetObject' in an <see cref="ErrorRecord"/>.</param>
        protected void WriteError(string message, ErrorCategory category, object targetObject) =>
            this.WriteError(new ArgumentException(message), category, targetObject);

        /// <summary>
        /// /// Issues a <see cref="PSCmdlet"/>.WriteError from a given string message, base <see cref="Exception"/>, <see cref="ErrorCategory"/>, and Target Object.
        /// </summary>
        /// <param name="message">The exception message to be displayed in the <see cref="ErrorRecord"/>.</param>
        /// <param name="exception">The exception whose InnerException will be become the InnerException of the <see cref="ErrorRecord"/> and its type will be used as the FullyQualifiedErrorId.</param>
        /// <param name="category">The category of the error.</param>
        /// <param name="targetObject">The object used as the 'targetObject' in an <see cref="ErrorRecord"/>.</param>
        protected void WriteError(string message, Exception exception, ErrorCategory category, object targetObject)
        {
            exception = this.GetAbsoluteException(exception);

            var errRec = new ErrorRecord(new InvalidOperationException(message, exception), exception.GetType().FullName, category, targetObject);
            base.WriteError(errRec);
        }

        /// <summary>
        /// Issues a <see cref="PSCmdlet"/>.WriteError from a given base exception and <see cref="ErrorCategory"/>.
        /// </summary>
        /// <param name="baseException">The base exception will be become the InnerException of the <see cref="ErrorRecord"/> and its type will be used as the FullyQualifiedErrorId.</param>
        /// <param name="category"></param>
        protected void WriteError(Exception baseException, ErrorCategory category) => this.WriteError(baseException, category, null);

        /// <summary>
        /// Issues a <see cref="PSCmdlet"/>.WriteError from a base <see cref="Exception"/>, <see cref="ErrorCategory"/>, and Target Object.
        /// </summary>
        /// <param name="message">The exception message to be displayed in the <see cref="ErrorRecord"/>.</param>
        /// <param name="baseException">The base exception will be become the InnerException of the <see cref="ErrorRecord"/> and its type will be used as the FullyQualifiedErrorId.</param>
        /// <param name="category">The category of the error.</param>
        /// <param name="targetObject">The object used as the 'targetObject' in an <see cref="ErrorRecord"/>.</param>
        protected void WriteError(Exception baseException, ErrorCategory category, object targetObject)
        {
            var errRec = new ErrorRecord(baseException, baseException.GetType().FullName, category, targetObject);
            base.WriteError(errRec);
        }

        protected void WriteError(IRestResponseDetails details, object targetObj = null)
        {
            if (details.HasException)
            {
                this.WriteError(details.Exception, ErrorCategory.InvalidResult, targetObj);
            }
            else
            {
                this.WriteError(new ErrorRecord(new HttpRequestException(
                    string.Format("An unknown error occurred while sending the GET request - {0}", details.StatusCode.ToString())),
                    "MG.Sonarr.UnknownHttpException", ErrorCategory.InvalidOperation, targetObj));
            }
        }

        #endregion
    }
}
