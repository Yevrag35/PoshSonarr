using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MG.Sonarr.Results
{
    public class Update : BaseResult
    {
        [JsonExtensionData]
        private IDictionary<string, JToken> _additionalData;

        public string Branch { get; set; }
        public string[] ChangesFixed { get; private set; }
        public string[] ChangesNew { get; private set; }
        public string FileName { get; set; }
        public string Hash { get; set; }
        public bool Installed { get; set; }
        public bool Installable { get; set; }
        public bool Latest { get; set; }
        public Uri Url { get; set; }

    }
}
