using MG.Sonarr.Next.Services.Metadata;
using Microsoft.Extensions.DependencyInjection;

namespace MG.Sonarr.Next.Shell.Context
{
    internal static class MetadataHandler
    {
        internal static void AddMetadata(IServiceCollection services)
        {
            MetadataResolver dict = new(12)
            {
                { Meta.BACKUP, Constants.BACKUP, false, new string[] { "Save-SonarrBackup" } },
                { Meta.CALENDAR, Constants.CALENDAR, false, new string[] { Meta.SERIES, Meta.EPISODE } },
                { Meta.DELAY_PROFILE, Constants.DELAY_PROFILE, true, new string[] { Meta.TAG } },
                { Meta.EPISODE, Constants.EPISODE, true, new string[] { Meta.SERIES, Meta.EPISODE_FILE } },
                { Meta.EPISODE_FILE, Constants.EPISODEFILE, true, new string[] { Meta.EPISODE } },
                { Meta.INDEXER, Constants.INDEXER, true },
                { Meta.LANGUAGE, Constants.LANGUAGE_PROFILE, true },
                { Meta.QUALITY_DEFINITION, Constants.QUALITY_DEFINITIONS, true },
                { Meta.QUALITY_PROFILE, Constants.PROFILE, true },
                { Meta.RELEASE_PROFILE, Constants.RELEASE_PROFILE, true, new string[] { Meta.TAG } },
                { Meta.ROOT_FOLDER, Constants.ROOTFOLDER, true },
                { Meta.SERIES, Constants.SERIES, true, new string[] { Meta.TAG, Meta.EPISODE, Meta.QUALITY_PROFILE } },
                { Meta.SERIES_ADD, Constants.SERIES + "/lookup", false },
                { Meta.TAG, Constants.TAG, true },
            };
            
            services.AddSingleton(dict);
        }
    }
}
