using MG.Sonarr.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "Notification", ConfirmImpact = ConfirmImpact.None, DefaultParameterSetName = "ByName")]
    [OutputType(typeof(Notification))]
    [Alias("Get-Connection")]
    [CmdletBinding(PositionalBinding = false)]
    public class GetNotification : NotificationCmdlet
    {
        #region PARAMETERS
        [Parameter(Mandatory = false, Position = 0, ParameterSetName = "ByName")]
        [SupportsWildcards]
        public string[] Name { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "ById")]
        public int[] Id { get; set; }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            if ( ! base.HasParameterSpecified(this, x => x.Id) && base.TryGetAllNotifications(out List<Notification> allNotifs))
            {
                base.SendToPipeline(base.FilterByStringParameter(allNotifs, p => p.Name, this, cmd => cmd.Name));
            }
            else if (base.HasParameterSpecified(this, x => x.Id))
            {
                base.SendToPipeline(base.GetNotificationsById(this.Id));
            }
        }

        #endregion
    }
}