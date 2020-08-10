using MG.Sonarr.Functionality;
using MG.Sonarr.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace MG.Sonarr.Cmdlets
{
    public abstract class NotificationCmdlet : BaseSonarrCmdlet
    {
        #region FIELDS/CONSTANTS

        #endregion

        #region PARAMETERS


        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing() => base.BeginProcessing();

        #endregion

        #region BACKEND METHODS
        protected private bool TryGetAllNotifications(out List<Notification> allNotifs)
        {
            allNotifs = base.SendSonarrListGet<Notification>(Endpoint.Notification);
            return allNotifs != null && allNotifs.Count > 0;
        }
        protected private IEnumerable<Notification> GetNotificationsById(int[] ids)
        {
            Endpoint ep = Endpoint.Notification;
            foreach (int oneId in ids)
            {
                Notification oneNotif = base.SendSonarrGet<Notification>(ep.WithId(oneId));
                if (oneNotif != null)
                    yield return oneNotif;
            }
        }

        #endregion
    }
}