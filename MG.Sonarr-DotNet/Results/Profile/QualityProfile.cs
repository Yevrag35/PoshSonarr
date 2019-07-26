using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;

namespace MG.Sonarr.Results
{
    public class QualityProfile : BaseResult
    {
        public QualityDetails Cutoff { get; set; }
        public int Id { get; set; }
        public QualityItemCollection Items { get; set; }
        public string Language { get; set; }
        public string Name { get; set; }
    }

    public class QualityItem : BaseResult
    {
        public bool Allowed { get; set; }
        public QualityDetails Quality { get; set; }
    }
}
