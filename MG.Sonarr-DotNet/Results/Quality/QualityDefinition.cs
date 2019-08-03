using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Runtime.Serialization;

namespace MG.Sonarr.Results
{
    [Serializable]
    public class QualityDefinitionBase : BaseResult
    {
        public double? MaxSize { get; set; }
        public double? MinSize { get; set; }
        public string Title { get; set; }
        public int Weight { get; set; }
    }

    [Serializable]
    public class PostQualityDefinition : QualityDefinitionBase
    {
        public QualityDetails Quality { get; set; }

        public static explicit operator PostQualityDefinition(QualityDefinition qualityDef)
        {
            return new PostQualityDefinition
            {
                MaxSize = qualityDef.MaxSize,
                MinSize = qualityDef.MinSize,
                Title = qualityDef.Title,
                Weight = qualityDef.Weight,
                Quality = new QualityDetails
                {
                    Id = qualityDef.Id,
                    Name = qualityDef.Name,
                    Resolution = qualityDef.Resolution,
                    Source = qualityDef.Source
                }
            };
        }
    }

    public class QualityDefinition : QualityDefinitionBase
    {
        [JsonExtensionData]
        private IDictionary<string, JToken> _additionalData;

        public int Id { get; private set; }
        public string Name { get; private set; }
        public string Source { get; private set; }
        public int Resolution { get; private set; }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            var token = _additionalData["quality"];
            if (token != null)
            {
                var tokId = token.SelectToken("$.id");
                if (tokId != null)
                    this.Id = tokId.ToObject<int>();

                var tokName = token.SelectToken("$.name");
                if (tokName != null)
                    this.Name = tokName.ToObject<string>();

                var tokSource = token.SelectToken("$.source");
                if (tokSource != null)
                    this.Source = tokSource.ToObject<string>();

                var tokRes = token.SelectToken("$.resolution");
                if (tokRes != null)
                    this.Resolution = tokRes.ToObject<int>();
            }
        }
    }
}
