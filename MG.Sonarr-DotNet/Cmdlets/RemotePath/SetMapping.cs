using MG.Sonarr.Results;
using System;
using System.Collections.Generic;
using System.Management.Automation;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsCommon.Set, "Mapping", ConfirmImpact = ConfirmImpact.None, SupportsShouldProcess = true)]
    [OutputType(typeof(RemotePathMapping))]
    [CmdletBinding(PositionalBinding = false)]
    public class SetMapping : BaseSonarrCmdlet
    {
        #region FIELDS/CONSTANTS
        private const string EP = "/remotepathmapping";
        private bool _force = false;
        private bool _passThru = false;

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true)]
        [Alias("MappingId")]
        public int Id { get; set; }

        [Parameter(Mandatory = true, Position = 1, ValueFromPipelineByPropertyName = true)]
        [Alias("Host")]
        public string HostName { get; set; }

        [Parameter(Mandatory = true, Position = 2, ValueFromPipelineByPropertyName = true)]
        public string LocalPath { get; set; }

        [Parameter(Mandatory = true, Position = 3, ValueFromPipelineByPropertyName = true)]
        public string RemotePath { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter PassThru
        {
            get => _passThru;
            set => _passThru = value;
        }

        //[Parameter(Mandatory = false)]
        //public SwitchParameter Force
        //{
        //    get => _force;
        //    set => _force = value;
        //}

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            if (base.ShouldProcess(string.Format("Remote Path Mapping: {0}", this.Id), "Set"))
            {
                RemotePathMapping setMapping = base.SendSonarrPut<RemotePathMapping>(EP, this.GetBodyParameters());
                if (_passThru)
                    base.SendToPipeline(setMapping);
            }
        }

        #endregion

        #region BACKEND METHODS
        private RemotePathMapping GetBodyParameters() => RemotePathMapping.FormatNew(this.HostName, this.LocalPath, this.Id, this.RemotePath);

        #endregion
    }
}