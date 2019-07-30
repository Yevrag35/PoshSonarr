using MG.Sonarr.Results;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Reflection;
using System.Security;

namespace MG.Sonarr.Cmdlets.Releases
{
    [Cmdlet(VerbsCommon.Add, "Release", ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true)]
    [CmdletBinding(PositionalBinding = false)]
    [OutputType(typeof(Release))]
    public class AddRelease : BaseSonarrCmdlet
    {
        #region FIELDS/CONSTANTS
        private const string EP = "/release";
        private bool _passThru;

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true)]
        [Alias("guid")]
        public Uri ReleaseUrl { get; set; }

        [Parameter(Mandatory = true, Position = 1, ValueFromPipelineByPropertyName = true)]
        public int IndexerId { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter PassThru
        {
            get => _passThru;
            set => _passThru = value;
        }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            var dict = new Dictionary<string, object>(2)
            {
                { "guid", this.ReleaseUrl },
                { "indexerId", this.IndexerId }
            };
            string postJson = JsonConvert.SerializeObject(dict, Formatting.Indented);

            if (base.ShouldProcess(string.Format("Release - {0}", this.ReleaseUrl.ToString()), "Add"))
            {
                string jsonRes = base.TryPostSonarrResult(EP, postJson);

                if (!string.IsNullOrEmpty(jsonRes) && _passThru)
                {
                    var reses = SonarrHttpClient.ConvertToSonarrResults<Release>(jsonRes, out bool iso);
                    base.WriteObject(reses, true);
                }
            }
        }

        #endregion

        #region METHODS


        #endregion
    }
}