using MG.Sonarr.Functionality;
using MG.Sonarr.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace MG.Sonarr.Cmdlets
{
    public abstract class DelayProfileCmdlet : BaseIdEndpointCmdlet
    {
        #region FIELDS/CONSTANTS
        protected override string Endpoint => "/delayprofile";

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing() => base.BeginProcessing();

        #endregion

        #region BACKEND METHODS
        protected private List<DelayProfile> GetAllDelayProfiles() => base.SendSonarrListGet<DelayProfile>(this.Endpoint);
        protected private IEnumerable<DelayProfile> GetDelayProfileByIds(IEnumerable<int> ids)
        {
            foreach (int id in ids)
            {
                DelayProfile oneProf = base.SendSonarrGet<DelayProfile>(base.FormatWithId(id));
                if (oneProf != null)
                    yield return oneProf;
            }
        }
        protected private bool TryGetDelayProfileById(int id, out DelayProfile foundProfile)
        {
            foundProfile = base.SendSonarrGet<DelayProfile>(base.FormatWithId(id));
            return foundProfile != null;
        }

        #endregion
    }
}