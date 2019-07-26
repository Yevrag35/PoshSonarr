using MG.Sonarr.Results;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using System.Reflection;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsCommon.New, "Tag", ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true)]
    [OutputType(typeof(Tag))]
    [CmdletBinding(PositionalBinding = false)]
    public class NewTag : BaseSonarrCmdlet
    {
        #region FIELDS/CONSTANTS
        private const string EP = "/tag";

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = true, Position = 0)]
        public string Label { get; set; }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            var dict = new Dictionary<string, string>(1)
            {
                { "label", this.Label }
            };
            string jsonBody = JsonConvert.SerializeObject(dict, Formatting.Indented);

            string jsonRes = null;
            if (base.ShouldProcess(string.Format("Tag - {0}", this.Label), "New"))
            {
                try
                {
                    jsonRes = _api.SonarrPost(EP, jsonBody);
                }
                catch (Exception e)
                {
                    base.WriteError(e, ErrorCategory.InvalidResult);
                }

                if (!string.IsNullOrEmpty(jsonRes))
                {
                    Tag res = SonarrHttpClient.ConvertToSonarrResult<Tag>(jsonRes);
                    base.WriteObject(res);
                }
            }
        }

        #endregion
    }
}