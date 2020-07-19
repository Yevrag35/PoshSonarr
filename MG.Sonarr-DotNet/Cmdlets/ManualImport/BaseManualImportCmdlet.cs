using MG.Sonarr.Functionality;
using MG.Sonarr.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace MG.Sonarr.Cmdlets
{
    public class BaseManualImportCmdlet : BaseSonarrCmdlet
    {
        protected override void BeginProcessing() => base.BeginProcessing();
        protected override void ProcessRecord() => base.ProcessRecord();
        protected override void EndProcessing() => base.EndProcessing();

        protected IUrlParameterCollection FormUrl(string path)
        {
            return new UrlParameterCollection
            {
                { new FolderParameter(path) }
            };
        }

        protected List<ManualImport> GetPossibleImports(string path)
        {
            IUrlParameterCollection parameters = this.FormUrl(path);
            string url = string.Format("{0}{1}", ApiEndpoint.ManualImport, parameters.ToQueryString());

            return base.SendSonarrListGet<ManualImport>(url);
        }
    }
}