using MG.Posh.Extensions.Bound;
using MG.Sonarr.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace MG.Sonarr.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "QualityDefinition", ConfirmImpact = ConfirmImpact.None, DefaultParameterSetName = "ByName")]
    [Alias("Get-Quality")]
    [OutputType(typeof(QualityDefinition))]
    public class GetQualityDefinition : BaseSonarrCmdlet
    {
        //private DynamicLibrary _lib;
        //private const string PARAM = "Name";
        private Func<IEnumerable<QualityDefinition>> _func;

        [Parameter(Mandatory = false, Position = 0, ParameterSetName = "ByName")]
        public string[] Name { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "ById")]
        public int[] Id { get; set; }

        protected override void BeginProcessing() => base.BeginProcessing();

        protected override void ProcessRecord()
        {
            if (this.ContainsParameter(x => x.Id))
                _func = GetFromIds;

            else
                _func = GetFromNames;

            QualityDefinition[] defs = _func().ToArray();
            base.SendToPipeline(defs);
        }

        private IEnumerable<QualityDefinition> GetFromIds()
        {
            foreach (int id in this.Id)
            {
                yield return base.SendSonarrGet<QualityDefinition>(string.Format("/qualitydefinition/{0}", id));
            }
        }
        private IEnumerable<QualityDefinition> GetFromNames()
        {
            IEnumerable<QualityDefinition> defs = base.SendSonarrListGet<QualityDefinition>("/qualitydefinition");

            if (defs != null)
            {
                defs = base.FilterByStringParameter(defs, x => x.Title, this, x => x.Name);
            }
            return defs;
        }
    }
}
