using MG.Posh.Extensions.Bound;
using MG.Sonarr.Functionality;
using MG.Sonarr.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsCommon.Set, "DelayProfile", ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true, DefaultParameterSetName = "ViaPipeline")]
    [Alias("Update-DelayProfile")]
    [OutputType(typeof(DelayProfile))]
    [CmdletBinding(PositionalBinding = false)]
    public class SetDelayProfile : DelayProfileCmdlet
    {
        #region FIELDS/CONSTANTS
        private bool _enUse;
        private bool _enTor;
        private bool _passThru;

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = true, DontShow = true, ValueFromPipeline = true, ParameterSetName = "ViaPipeline")]
        public DelayProfile InputObject { get; set; }

        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "ByExplicitId")]
        public int Id { get; set; }

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

        [Parameter(Mandatory = false)]
        [ValidateRange(1, 2147483646)]
        public int Order { get; set; }

        [Parameter(Mandatory = false)]
        [ValidateSet("Unknown", "Torrent", "Usenet")]
        public string PreferredProtocol { get; set; }

        [Parameter(Mandatory = false)]
        public int TorrentDelay { get; set; }

        [Parameter(Mandatory = false)]
        public int UsenetDelay { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter PassThru
        {
            get => _passThru;
            set => _passThru = value;
        }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing()
        {
            base.BeginProcessing();
            if (this.ContainsParameter(x => x.Id))
            {
                if (base.TryGetDelayProfileById(this.Id, out DelayProfile foundProfile))
                {
                    this.InputObject = foundProfile;
                }
                else
                {
                    base.ThrowTerminatingError(new ErrorRecord(new ArgumentException(string.Format("No DelayProfile with an ID of {0} exists.", this.Id)),
                        "DelayProfileNotFound", ErrorCategory.ObjectNotFound, base.FormatWithId(this.Id)));
                }
            }
        }

        protected override void ProcessRecord()
        {
            this.SetParametersToObject();
            base.WriteDebug(this.InputObject.ToJson());
            if (base.FormatShouldProcess("Set", "DelayProfile Id: {0}", this.InputObject.Id))
            {
                DelayProfile modified = base.SendSonarrPut<DelayProfile>(base.Endpoint, this.InputObject);
                if (_passThru)
                    base.SendToPipeline(modified);
            }
        }

        #endregion

        #region BACKEND METHODS
        private void SetParametersToObject()
        {
            if (this.ContainsParameter(x => x.EnableTorrent))
                this.InputObject.EnableTorrent = _enTor;

            if (this.ContainsParameter(x => x.EnableUseNet))
                this.InputObject.EnableUsenet = _enUse;

            if (this.ContainsParameter(x => x.Order))
            {
                if (this.InputObject.IsDefault)
                    base.WriteError(new InvalidOperationException("Cannot set the order of the default delay profile."), ErrorCategory.InvalidOperation, this.InputObject);

                else
                    this.InputObject.SetOrder(this.Order);
            }

            if (this.ContainsParameter(x => x.PreferredProtocol))
                this.InputObject.PreferredProtocol = this.PreferredProtocol;

            if (this.ContainsParameter(x => x.TorrentDelay))
                this.InputObject.TorrentDelay = this.TorrentDelay;

            if (this.ContainsParameter(x => x.UsenetDelay))
                this.InputObject.UsenetDelay = this.UsenetDelay;
        }

        #endregion
    }
}