using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
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

        protected private static readonly JsonSerializerSettings Serializer = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            DateParseHandling = DateParseHandling.DateTime,
            DateTimeZoneHandling = DateTimeZoneHandling.Local,
            DefaultValueHandling = DefaultValueHandling.Populate,
            FloatParseHandling = FloatParseHandling.Decimal,
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore,
            PreserveReferencesHandling = PreserveReferencesHandling.Objects,
            ObjectCreationHandling = ObjectCreationHandling.Replace,
            ReferenceLoopHandling = ReferenceLoopHandling.Serialize
        };

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
        protected private void WriteError(string msg, ErrorCategory cat) =>
            this.WriteError(new ArgumentException(msg), cat, null);

        protected private void WriteError(string msg, ErrorCategory cat, object obj) =>
            this.WriteError(new ArgumentException(msg), cat, obj);

        protected private void WriteError(string msg, Exception exception, ErrorCategory cat, object obj)
        {
            var errRec = new ErrorRecord(new InvalidOperationException(msg, exception), exception.GetType().FullName, cat, obj);
            base.WriteError(errRec);
        }

        protected private void WriteError(Exception baseEx, ErrorCategory cat) => this.WriteError(baseEx, cat, null);
        protected private void WriteError(Exception baseEx, ErrorCategory cat, object obj)
        {
            var errRec = new ErrorRecord(baseEx, baseEx.GetType().FullName, cat, obj);
            base.WriteError(errRec);
        }

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