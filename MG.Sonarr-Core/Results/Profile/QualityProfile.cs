using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;

namespace MG.Sonarr.Results
{
    public class QualityProfile : BaseResult
    {
        public QualityItemDetails Cutoff { get; set; }
        public int Id { get; set; }
        public QualityItemCollection Items { get; set; }
        public string Language { get; set; }
        public string Name { get; set; }
    }

    public class QualityItem : BaseResult
    {
        public bool Allowed { get; set; }
        public QualityItemDetails Quality { get; set; }
    }

    public class QualityItemDetails : BaseResult
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Source { get; set; }
        public int Resolution { get; set; }
    }
}
