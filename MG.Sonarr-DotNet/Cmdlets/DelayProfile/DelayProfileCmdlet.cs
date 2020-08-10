using MG.Sonarr.Functionality;
using MG.Sonarr.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace MG.Sonarr.Cmdlets
{
    public abstract class DelayProfileCmdlet : BaseSonarrCmdlet
    {
        #region FIELDS/CONSTANTS

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing() => base.BeginProcessing();

        #endregion

        #region BACKEND METHODS
        protected private List<DelayProfile> GetAllDelayProfiles() => base.SendSonarrListGet<DelayProfile>(Endpoint.DelayProfile);
        protected private IEnumerable<DelayProfile> GetDelayProfileByIds(IEnumerable<int> ids)
        {
            Endpoint ep = Endpoint.DelayProfile;
            foreach (int id in ids)
            {
                DelayProfile oneProf = base.SendSonarrGet<DelayProfile>(ep.WithId(id));
                if (oneProf != null)
                    yield return oneProf;
            }
        }
        protected private bool TryGetDelayProfileById(int id, out DelayProfile foundProfile)
        {
            foundProfile = base.SendSonarrGet<DelayProfile>(Endpoint.DelayProfile.WithId(id));
            return foundProfile != null;
        }

        #endregion
    }
}