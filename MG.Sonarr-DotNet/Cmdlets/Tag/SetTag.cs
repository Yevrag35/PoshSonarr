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
    [Cmdlet(VerbsCommon.Set, "Tag", ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true)]
    [OutputType(typeof(Tag))]
    [CmdletBinding(PositionalBinding = false)]
    public class SetTag : BaseSonarrCmdlet
    {
        #region FIELDS/CONSTANTS
        private const string EP = "/tag";
        private bool _passThru;

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true)]
        public int Id { get; set; }

        [Parameter(Mandatory = true, Position = 1)]
        public string NewLabel { get; set; }

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
                { "id", this.Id },
                { "label", this.NewLabel }
            };
            string jsonBody = JsonConvert.SerializeObject(dict, Formatting.Indented);

            if (base.ShouldProcess(string.Format("Tag - {0}", this.Id), "Set"))
            {
                string jsonRes = base.TryPutSonarrResult(EP, jsonBody);

                if (!string.IsNullOrEmpty(jsonRes) && _passThru)
                {
                    Tag res = SonarrHttpClient.ConvertToSonarrResult<Tag>(jsonRes);
                    base.WriteObject(res);
                }
            }
        }

        #endregion
    }
}