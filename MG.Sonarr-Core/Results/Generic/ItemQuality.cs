using System;
using System.Collections.Generic;

namespace MG.Sonarr.Results
{
    public class ItemQuality : BaseResult
    {
        public QualityDetails Quality { get; set; }
        public Revision Revision { get; set; }
    }
}
