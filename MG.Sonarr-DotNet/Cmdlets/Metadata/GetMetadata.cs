using MG.Sonarr.Functionality;
using MG.Sonarr.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "Metadata", ConfirmImpact = ConfirmImpact.None, DefaultParameterSetName = "None")]
    [OutputType(typeof(Metadata))]
    [CmdletBinding(PositionalBinding = false)]
    public class GetMetadata : BaseSonarrCmdlet
    {
        #region FIELDS/CONSTANTS
        private const string EP = "/metadata";
        private const string EP_WITH_ID = EP + "/{0}";

        #endregion

        #region PARAMETERS
        [Parameter(Mandatory = false, ParameterSetName = "MetadataById")]
        public int[] Id { get; set; }

        [Parameter(Mandatory = false, ParameterSetName = "MetadataByName")]
        [SupportsWildcards]
        public string[] Name { get; set; }

        #endregion

        #region CMDLET PROCESSING
        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            if (base.HasParameterSpecified(this, x => x.Id))
                base.SendToPipeline(this.GetMetadataById(this.Id));

            else
            {
                List<Metadata> all = base.SendSonarrListGet<Metadata>(EP);
                all.Sort();
                if (base.HasParameterSpecified(this, x => x.Name))
                    base.SendToPipeline(base.FilterByStringParameter(all, m => m.Name, this, cmd => cmd.Name));

                else
                    base.SendToPipeline(all);
            }
        }

        #endregion

        #region BACKEND METHODS
        protected private List<Metadata> GetAllMetadata() => base.SendSonarrListGet<Metadata>(EP);
        protected private IEnumerable<Metadata> GetMetadataById(int[] ids)
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