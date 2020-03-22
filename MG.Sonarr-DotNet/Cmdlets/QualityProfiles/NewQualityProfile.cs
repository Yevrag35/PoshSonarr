using MG.Dynamic;
using MG.Sonarr.Functionality;
using MG.Sonarr.Results;
using System;
using System.Collections.Generic;
using System.Management.Automation;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsCommon.New, "QualityProfile", ConfirmImpact = ConfirmImpact.None, SupportsShouldProcess = true)]
    [OutputType(typeof(QualityProfile))]
    [Alias("New-Profile")]
    [CmdletBinding(PositionalBinding = false)]
    public class NewQualityProfile : BaseSonarrCmdlet, IDynamicParameters
    {
        #region FIELDS/CONSTANTS
        private const string EP = "/profile";
        private DynamicLibrary _dynLib;
        private const string CUTOFF_QUALITY = "CutoffQuality";
        private const string ALLOWED_QUALITIES = "AllowedQualities";

        private const string WHAT_IF_FORMAT = "{0} Profile \"{1}\" with {2} allowed qualities and {3} as the cutoff.";

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = true, Position = 0)]
        public string Name { get; set; }

        [Parameter(Mandatory = false)]
        public ProfileLanugage Language { get; set; } = ProfileLanugage.English;

        #endregion

        #region DYNAMIC
        public object GetDynamicParameters()
        {
            if (Context.IsConnected && _dynLib == null)
            {
                _dynLib = new DynamicLibrary
                {
                    new DynamicParameter<Quality>(CUTOFF_QUALITY, false, Context.AllQualities, x => x.Name)
                    {
                        Mandatory = true
                    },
                    new DynamicParameter<Quality>(ALLOWED_QUALITIES, true, Context.AllQualities, x => x.Name)
                    {
                        Mandatory = true
                    }
                };
            }
            return _dynLib;
        }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            Quality cutoff = _dynLib.GetUnderlyingValue<Quality>(CUTOFF_QUALITY);
            var newProfile = new QualityProfileNew
            {
                Cutoff = cutoff,
                Language = this.Language,
                Name = this.Name
            };
            this.SetAllowedQualities(newProfile, _dynLib.GetUnderlyingValues<Quality>(ALLOWED_QUALITIES));

            if (base.FormatShouldProcess("New", WHAT_IF_FORMAT, this.Language.ToString(), this.Name, newProfile.AllowedQualities.Count, cutoff.Name))
            {
                QualityProfile createdProfile = base.SendSonarrPost<QualityProfile>(EP, newProfile);
                base.SendToPipeline(createdProfile);
            }
        }

        #endregion

        #region BACKEND METHODS
        private void SetAllowedQualities(QualityProfileNew newProfile, IEnumerable<Quality> qualities)
        {
            foreach (Quality quality in qualities)
            {
                newProfile.AllowedQualities.AddFromQuality(quality, true);
            }
        }

        #endregion
    }
}