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

namespace MG.Sonarr.Cmdlets.Profiles
{
    [Cmdlet(VerbsCommon.Get, "QualityProfile", ConfirmImpact = ConfirmImpact.None, DefaultParameterSetName = "ByProfileName")]
    [CmdletBinding(PositionalBinding = false)]
    [OutputType(typeof(QualityProfile))]
    public class GetProfile : BaseSonarrCmdlet
    {
        #region FIELDS/CONSTANTS


        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = false, Position = 0, ParameterSetName = "ByProfileName")]
        public string[] Name { get; set; }

        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "ByProfileId", ValueFromPipelineByPropertyName = true)]
        [Alias("QualityProfileId")]
        public int[] Id { get; set; }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            if ( ! base.HasParameterSpecified(this, x => x.Id))
            {
                if (this.TryGetAllProfiles(out List<QualityProfile> profs))
                {
                    base.SendToPipeline(base.FilterByStringParameter(profs, p => p.Name, this, cmd => cmd.Name));
                }
            }
            else
            {
                foreach (int singleId in this.Id)
                {
                    string qpIdEndpoint = this.GetProfileIdEndpoint(singleId);
                    base.SendToPipeline(base.SendSonarrGet<QualityProfile>(qpIdEndpoint));
                }
            }
        }

        #endregion

        #region METHODS

        private string GetProfileIdEndpoint(int id) => string.Format("/profile/{0}", id);

        private bool TryGetAllProfiles(out List<QualityProfile> outProfiles)
        {
            outProfiles = base.SendSonarrListGet<QualityProfile>("/profile");
            return outProfiles != null && outProfiles.Count > 0;
        }

        #endregion
    }
}