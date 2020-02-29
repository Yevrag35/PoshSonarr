using MG.Posh.Extensions.Bound;
using MG.Sonarr.Functionality;
using MG.Sonarr.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsCommon.New, "DelayProfile", ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true)]
    [OutputType(typeof(DelayProfile))]
    [CmdletBinding(PositionalBinding = false)]
    public class NewDelayProfile : DelayProfileCmdlet
    {
        #region FIELDS/CONSTANTS
        private bool _enUse;
        private bool _enTor;

        #endregion

        #region PARAMETERS
        

        [Parameter(Mandatory = false)]
        public SwitchParameter EnableTorrent
        {
            get => _enTor;
            set => _enTor = value;
        }

        [Parameter(Mandatory = false)]
        public SwitchParameter EnableUseNet
        {
            get => _enUse;
            set => _enUse = value;
        }

        [Parameter(Mandatory = true, Position = 1)]
        [ValidateRange(1, 2147483646)]
        public int Order { get; set; }

        [Parameter(Mandatory = false)]
        public DownloadProtocol PreferredProtocol { get; set; }

        [Parameter(Mandatory = false)]
        public int TorrentDelay { get; set; }

        [Parameter(Mandatory = false)]
        public int UsenetDelay { get; set; }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            if ( ! this.ContainsAnyParameters(x => x.EnableTorrent, x => x.EnableUseNet))
            {
                base.ThrowTerminatingError("Either Torrent or Usenet must be enabled.", ErrorCategory.InvalidArgument);
            }
        }

        protected override void ProcessRecord()
        {
            var post = DelayProfilePost.NewPost(_enTor, _enUse, this.Order, this.PreferredProtocol, this.TorrentDelay, this.UsenetDelay, null);
            base.WriteDebug(post.ToJson());
            if (base.ShouldProcess("DelayProfile", "New"))
            {
                DelayProfile dp = base.SendSonarrPost<DelayProfile>(base.Endpoint, post);
                base.SendToPipeline(dp);
            }
        }

        #endregion
    }
}