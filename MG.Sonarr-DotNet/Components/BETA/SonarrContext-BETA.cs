using MG.Sonarr.Results;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MG.Sonarr
{
    public static partial class Context
    {
        public static List<QualityDefinition> Qualities { get; set; }
    }
}
