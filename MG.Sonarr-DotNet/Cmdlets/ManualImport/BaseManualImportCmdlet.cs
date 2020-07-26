using MG.Sonarr.Functionality.Collections;
using MG.Sonarr.Functionality.Strings;
using MG.Sonarr.Functionality.Url;
using MG.Sonarr.Results;
using System;
using System.Collections.Generic;

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
            string url = string.Format("{0}{1}", ApiEndpoints.ManualImport, parameters.ToQueryString());

            return base.SendSonarrListGet<ManualImport>(url);
        }
    }
}