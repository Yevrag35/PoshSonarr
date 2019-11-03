using System;
using System.Collections.Generic;

namespace MG.Sonarr.Results
{ 
    public class AlternateTitle : BaseResult
    {
        public int? SceneSeasonNumber { get; set; }
        public int? SeasonNumber { get; set; }
        public string Title { get; set; }

        public AlternateTitle() { }
    }
}
