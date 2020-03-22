using MG.Sonarr.Results;
using System;
using System.Collections.Generic;
using System.Management.Automation;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsCommon.New, "ReleasePush", ConfirmImpact = ConfirmImpact.None, SupportsShouldProcess = true)]
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
            SonarrBodyParameters bodyParameters = this.GetBodyParameters();

            if (base.ShouldProcess(string.Format("New Release Push - {0}", this.Title), "New"))
            {
                List<Release> release = base.SendSonarrListPost<Release>(EP, bodyParameters);
                base.SendToPipeline(release);
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

        private SonarrBodyParameters GetBodyParameters()
        {
            return new SonarrBodyParameters(4)
            {
                { "title", this.Title },
                { "downloadUrl", this.DownloadUrl },
                { "protocol", this.Protocol },
                { "publishDate", this.DateToISODate(this.PublishDate) }
            };
        }

        #endregion
    }
}