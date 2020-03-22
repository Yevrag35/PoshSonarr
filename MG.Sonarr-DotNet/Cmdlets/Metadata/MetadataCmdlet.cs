using MG.Sonarr.Functionality;
using MG.Sonarr.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace MG.Sonarr.Cmdlets
{
    public abstract class MetadataCmdlet : BaseSonarrCmdlet
    {
        #region FIELDS/CONSTANTS
        protected private const string EP = "/metadata";
        protected private const string EP_WITH_ID = EP + "/{0}";

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing() => base.BeginProcessing();

        #endregion

        #region BACKEND METHODS
        protected private List<Metadata> GetAllMetadata() => base.SendSonarrListGet<Metadata>(EP);
        protected private Metadata GetMetadataByName(string wildcardName, List<Metadata> allMetadata)
        {
            var wcp = new WildcardPattern(wildcardName, WildcardOptions.IgnoreCase);
            return allMetadata.Find(x => wcp.IsMatch(x.Name));
        }
        protected private IEnumerable<Metadata> GetMetadataById(params int[] ids)
        {
            foreach (int id in ids)
            {
                Metadata maybe = base.SendSonarrGet<Metadata>(string.Format(EP_WITH_ID, id));
                if (maybe != null)
                    yield return maybe;
            }
        }

        #endregion
    }
}