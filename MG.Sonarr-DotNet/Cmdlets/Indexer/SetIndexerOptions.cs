using MG.Posh.Extensions.Bound;
using MG.Sonarr.Functionality;
using MG.Sonarr.Results;
using System;
using System.Collections.Generic;
using System.Linq;
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
        [Parameter(Mandatory = false, ValueFromPipelineByPropertyName = true)]
        [ValidateRange(0, int.MaxValue)]
        [Alias("MaximumSizeInGB")]
        public int MaximumSize { get; set; }

        [Parameter(Mandatory = false, ValueFromPipelineByPropertyName = true)]
        [ValidateRange(0, int.MaxValue)]
        [Alias("MinimumAgeInMins")]
        public int MinimumAge { get; set; }

        [Parameter(Mandatory = false, ValueFromPipelineByPropertyName = true)]
        [Alias("RetentionInDays")]
        [ValidateRange(0, int.MaxValue)]
        public int Retention { get; set; }

        [Parameter(Mandatory = false, ValueFromPipelineByPropertyName = true)]
        [Alias("RssSyncIntervalInMins")]
        [ValidateRange(0, int.MaxValue)]
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
            IndexerOptions options = base.SendSonarrGet<IndexerOptions>(ApiEndpoint.IndexerOptions);
            if (this.ContainsParameter(x => x.MaximumSize))
            {
                _changed = true;
                options.MaximumSizeInGB = this.MaximumSize;
            }
            if (this.ContainsParameter(x => x.MinimumAge))
            {
                _changed = true;
                options.MinimumAgeInMins = this.MinimumAge;
            }    
            if (this.ContainsParameter(x => x.Retention))
            {
                _changed = true;
                options.RetentionInDays = this.Retention;
            }
            if (this.ContainsParameter(x => x.RssSyncInterval))
            {
                _changed = true;
                options.RssSyncIntervalInMins = this.RssSyncInterval;
            }

            if (_changed && base.ShouldProcess("Indexer Options", "Set"))
            {
                IndexerOptions changed = base.SendSonarrPut<IndexerOptions>(ApiEndpoint.IndexerOptions, options);
                if (_passThru)
                    base.SendToPipeline(changed);
            }
        }

        #endregion
    }
}