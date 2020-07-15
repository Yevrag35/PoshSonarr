using MG.Sonarr.Functionality;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MG.Sonarr.Results
{
    public static class ManualImportPost
    {
        public static SonarrBodyParameters NewManualImportObject(ManualImport import)
        {
            return NewManualImportObject(new ManualImport[1] { import });
        }
        public static SonarrBodyParameters NewManualImportObject(IEnumerable<ManualImport> imports)
        {
            return new SonarrBodyParameters
            {
                { "files", new JArray(imports.Select(x => x.PostThis()).ToArray()) },
                { "importMode", "Move" },
                { "name", "manualImport" }
            };
        }
    }
}
