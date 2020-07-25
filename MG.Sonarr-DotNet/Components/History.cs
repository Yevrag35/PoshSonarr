using MG.Sonarr.Functionality;
using MG.Sonarr.Results;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace MG.Sonarr
{
    internal static class History
    {
        internal static IJobHistory Jobs { get; private set; }

        internal static void Initialize() => Jobs = SonarrFactory.NewJobHistory();
        internal static bool IsInitialized() => Jobs != null;
    }
}
