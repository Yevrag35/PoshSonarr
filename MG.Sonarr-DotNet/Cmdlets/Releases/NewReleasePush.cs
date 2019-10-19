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

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsCommon.New, "ReleasePush", ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true)]
    [CmdletBinding(PositionalBinding = false)]
    [OutputType(typeof(Release))]
    public class NewReleasePush : BaseSonarrCmdlet
    {
        #region FIELDS/CONSTANTS
        private const string EP = "/release";
        private const string ISO_DATE = "yyyy-MM-ddTHH:mm:ssZ";
        private bool _passThru;

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = true, Position = 0)]
        public string Title { get; set; }

        [Parameter(Mandatory = true, Position = 1)]
        public Uri DownloadUrl { get; set; }

        [Parameter(Mandatory = true, Position = 2)]
        [ValidateSet("Usenet", "Torrent")]
        public string Protocol { get; set; }

        [Parameter(Mandatory = true, Position = 3)]
        public DateTime PublishDate { get; set; }

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
            var dict = new Dictionary<string, object>(4)
            {
                { "title", this.Title },
                { "downloadUrl", this.DownloadUrl },
                { "protocol", this.Protocol },
                { "publishDate", this.DateToISODate(this.PublishDate) }
            };
            string postJson = JsonConvert.SerializeObject(dict, Formatting.Indented);
            if (base.ShouldProcess(string.Format("New Release Push - {0}", this.Title), "New"))
            {
                string jsonRes = base.TryPostSonarrResult(EP, postJson);

                if (!string.IsNullOrEmpty(jsonRes) && _passThru)
                {
                    List<Release> reses = SonarrHttp.ConvertToSonarrResults<Release>(jsonRes, out bool iso);
                    base.WriteObject(reses, true);
                }
            }
        }

        #endregion

        #region METHODS
        private string DateToISODate(DateTime dt)
        {
            if (dt.Kind != DateTimeKind.Utc)
            {
                dt = dt.ToUniversalTime();
            }
            return dt.ToString(ISO_DATE);
        }

        #endregion
    }
}