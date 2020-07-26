using MG.Posh.Extensions.Bound;
using MG.Sonarr.Functionality.Strings;
using MG.Sonarr.Results;
using System;
using System.Management.Automation;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsCommon.Set, "IndexerOptions", ConfirmImpact = ConfirmImpact.Low, SupportsShouldProcess = true)]
    [OutputType(typeof(IndexerOptions))]
    public class SetIndexerOptions : BaseSonarrCmdlet
    {
        private bool _changed;
        private bool _passThru;

        #region PARAMETERS
        [Parameter(Mandatory = false, ValueFromPipeline = true)]
        public IndexerOptions InputObject { get; set; }

        [Parameter(Mandatory = false)]
        [ValidateRange(0, int.MaxValue)]
        [Alias("MaximumSizeInGB")]
        public int MaximumSize { get; set; }

        [Parameter(Mandatory = false)]
        [ValidateRange(0, int.MaxValue)]
        [Alias("MinimumAgeInMins")]
        public int MinimumAge { get; set; }

        [Parameter(Mandatory = false)]
        [Alias("RetentionInDays")]
        [ValidateRange(0, int.MaxValue)]
        public int Retention { get; set; }

        [Parameter(Mandatory = false)]
        [Alias("RssSyncIntervalInMins")]
        //[ValidateRange(0, int.MaxValue)]
        public int RssSyncInterval { get; set; }

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
            if (!this.ContainsParameter(x => x.InputObject))
            {
                this.InputObject = base.SendSonarrGet<IndexerOptions>(ApiEndpoints.IndexerOptions);
            }

            if (this.ContainsParameter(x => x.MaximumSize))
            {
                _changed = true;
                this.InputObject.MaximumSizeInGB = this.MaximumSize;
            }
            if (this.ContainsParameter(x => x.MinimumAge))
            {
                _changed = true;
                this.InputObject.MinimumAgeInMins = this.MinimumAge;
            }    
            if (this.ContainsParameter(x => x.Retention))
            {
                _changed = true;
                this.InputObject.RetentionInDays = this.Retention;
            }
            if (this.ContainsParameter(x => x.RssSyncInterval))
            {
                _changed = true;
                this.InputObject.RssSyncIntervalInMins = this.RssSyncInterval;
            }

            if (_changed && base.ShouldProcess("Indexer Options", "Set"))
            {
                IndexerOptions changed = base.SendSonarrPut<IndexerOptions>(ApiEndpoints.IndexerOptions, this.InputObject);
                if (_passThru)
                    base.SendToPipeline(changed);
            }
            else if (!_changed)
            {
                base.WriteWarning("No Indexer options were updated as no difference was detected.");
            }
        }

        #endregion
    }
}