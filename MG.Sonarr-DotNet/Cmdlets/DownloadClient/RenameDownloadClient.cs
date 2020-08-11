using MG.Posh.Extensions.Shoulds;
using MG.Posh.Extensions.Writes;
using MG.Sonarr.Functionality;
using MG.Sonarr.Results;
using System;
using System.Collections.Generic;
using System.Management.Automation;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsCommon.Rename, "DownloadClient", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Low)]
    [OutputType(typeof(DownloadClient))]
    public class RenameDownloadClient : BaseSonarrCmdlet
    {
        #region FIELDS/CONSTANTS
        private bool _passThru;

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = true, ValueFromPipeline = true)]
        [Alias("DownloadClient")]
        public DownloadClient InputObject { get; set; }

        [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true)]
        public string NewName { get; set; }

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
            if (string.IsNullOrWhiteSpace(this.NewName))
                this.WriteArgumentError("The new name cannot be null, empty, or whitespace.", ErrorCategory.InvalidArgument, this.InputObject);
            
            if (this.ShouldProcessFormat("Rename", "DownloadClient {0}: {1} => {2}", this.InputObject.Id, this.InputObject.Name, this.NewName))
            {
                DownloadClient edited = DownloadClient.Rename(this.NewName, this.InputObject);
                base.SendSonarrPut<DownloadClient>(Endpoint.DownloadClient.WithId(this.InputObject.Id), edited);
                if (_passThru)
                    base.WriteObject(edited);
            }
        }

        #endregion
    }
}