using MG.Sonarr.Functionality;
using MG.Sonarr.Functionality.Jobs;
using System;

namespace MG.Sonarr
{
    internal static class History
    {
        internal static IJobHistory Jobs { get; private set; }

        internal static void Initialize() => Jobs = SonarrFactory.NewJobHistory();
        internal static bool IsInitialized() => Jobs != null;
    }
}
