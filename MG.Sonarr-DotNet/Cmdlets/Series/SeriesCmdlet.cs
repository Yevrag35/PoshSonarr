using MG.Sonarr.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace MG.Sonarr.Cmdlets
{
    public abstract class SeriesCmdlet : BaseIdEndpointCmdlet
    {
        #region FIELDS/CONSTANTS
        protected override string Endpoint => "/series";

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing() => base.BeginProcessing();

        #endregion

        #region BACKEND METHODS
        protected private List<SeriesResult> GetAllSeries(bool debugging)
        {
            if (!debugging)
                base.WriteFormatVerbose("Retrieving all series from {0}.", this.Endpoint);

            List<SeriesResult> allSeries = base.SendSonarrListGet<SeriesResult>(this.Endpoint);

            if (debugging)
                base.WriteFormatVerbose("Found {0} series.", allSeries.Count);

            return allSeries;
        }

        protected private IEnumerable<SeriesResult> GetSeriesById(IEnumerable<int> ids, bool debugging)
        {
            foreach (int id in ids)
            {
                SeriesResult sr = base.SendSonarrGet<SeriesResult>(base.FormatWithId(id));
                if (sr != null)
                {
                    if (debugging)
                        base.WriteFormatDebug("Found series with ID \"{0}\" - {1}", id, sr.CleanTitle);

                    yield return sr;
                }
            }
        }

        #endregion
    }
}