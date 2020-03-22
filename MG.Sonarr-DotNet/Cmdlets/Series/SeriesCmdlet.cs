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
        protected private List<SeriesResult> GetAllSeries() => base.SendSonarrListGet<SeriesResult>(this.Endpoint);

        protected private IEnumerable<SeriesResult> GetSeriesById(IEnumerable<int> ids)
        {
            foreach (int id in ids)
            {
                SeriesResult sr = base.SendSonarrGet<SeriesResult>(base.FormatWithId(id));
                if (sr != null)
                    yield return sr;
            }
        }

        #endregion
    }
}