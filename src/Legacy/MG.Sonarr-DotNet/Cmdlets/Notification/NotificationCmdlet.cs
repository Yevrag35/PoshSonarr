using MG.Sonarr.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace MG.Sonarr.Cmdlets
{
    public abstract class NotificationCmdlet : BaseIdEndpointCmdlet
    {
        #region FIELDS/CONSTANTS
        protected override string Endpoint => "/notification";

        #endregion

        #region PARAMETERS


        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing() => base.BeginProcessing();

        #endregion

        #region BACKEND METHODS
        protected private bool TryGetAllNotifications(out List<Notification> allNotifs)
        {
            allNotifs = base.SendSonarrListGet<Notification>(this.Endpoint);
            return allNotifs != null && allNotifs.Count > 0;
        }
        protected private IEnumerable<Notification> GetNotificationsById(int[] ids)
        {
            foreach (int oneId in ids)
            {
                string endpoint = base.FormatWithId(oneId);
                Notification oneNotif = base.SendSonarrGet<Notification>(endpoint);
                if (oneNotif != null)
                    yield return oneNotif;
            }
        }

        #endregion
    }
}