using MG.Sonarr.Results;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Reflection;
using System.Security;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "Calendar", ConfirmImpact = ConfirmImpact.None, DefaultParameterSetName = "None")]
    [CmdletBinding(PositionalBinding = false)]
    [OutputType(typeof(CalendarEntry))]
    public class GetCalendar : BaseSonarrCmdlet
    {
        #region FIELDS/CONSTANTS
        private const string DT_FORMAT = "yyyy-MM-dd";
        private const string EP = "/calendar";
        private const string EP_WITH_DATE = EP + "?start={0}&end={1}";

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = true, ParameterSetName = "WithStartEndDate")]
        public DateTime StartDate { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "WithStartEndDate")]
        public DateTime EndDate { get; set; }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            string full = EP;
            if (this.ParameterSetName == "WithStartEndDate")
            {
                string start = this.DateToString(this.StartDate);
                string end = this.DateToString(this.EndDate);
                full = string.Format(EP_WITH_DATE, start, end);
            }

            string jsonRes = base.TryGetSonarrResult(full);
            if (!string.IsNullOrEmpty(jsonRes))
            {
                var entries = SonarrHttpClient.ConvertToSonarrResults<CalendarEntry>(jsonRes, out bool iso);
                for (int i = 0; i < entries.Count; i++)
                {
                    var entry = entries[i];
                    if (entry.AirDateUtc.HasValue)
                    {
                        entry.AirDateUtc = entry.AirDateUtc.Value.ToUniversalTime();
                    }
                }

                base.WriteObject(entries, true);
            }
        }

        #endregion

        #region METHODS
        private string DateToString(DateTime dt) => dt.ToString(DT_FORMAT);

        #endregion
    }
}