﻿using Microsoft.Extensions.DependencyInjection;

namespace MG.Sonarr.Next.Metadata
{
    public static class MetadataResolverDI
    {
        public static IServiceCollection AddMetadata(this IServiceCollection services)
        {
            int initialCapacity = 22;
            MetadataResolver dict = new(initialCapacity)
            {
                { Meta.BACKUP, Constants.BACKUP, true, new string[] { "Remove-SonarrBackup", "Save-SonarrBackup" } },
                { Meta.CALENDAR, Constants.CALENDAR, false, new string[] { "Get-SonarrSeries", "Get-SonarrEpisode", "Get-SonarrEpisodeFile", "Invoke-SonarrRename", "Remove-SonarrEpisodeFile" } },
                { Meta.COMMAND, Constants.COMMAND, true },
                { Meta.DELAY_PROFILE, Constants.DELAY_PROFILE, true, new string[] { "Get-SonarrTag", "Add-SonarrTag", "Clear-SonarrTag" } },
                { Meta.DOWNLOAD_CLIENT, Constants.DOWNLOAD_CLIENT, true, new string[] { "Test-SonarrResource" } },
                { Meta.DOWNLOAD_CLIENT_CONFIG, Constants.DOWNLOAD_CLIENT_CONFIG, true, new string[] { "Update-SonarrDownloadClientConfig" } },
                { Meta.EPISODE, Constants.EPISODE, true, new string[] { "Get-SonarrEpisodeFile", "Remove-SonarrEpisodeFile", "Get-SonarrRelease", "Get-SonarrSeries", "Invoke-SonarrRename" } },
                { Meta.EPISODE_FILE, Constants.EPISODEFILE, true, new string[] { "Get-SonarrEpisodeFile", 
                    "Invoke-SonarrRename", "Remove-SonarrEpisodeFile" } },
                { Meta.HOST, Constants.HOST, true, new string[] { "Update-SonarrHostConfig" } },
                { Meta.INDEXER, Constants.INDEXER, true, new string[] { "Get-SonarrTag", "Add-SonarrTag", "Remove-SonarrTag", "Test-SonarrResource" } },
                { Meta.LANGUAGE, Constants.LANGUAGE_PROFILE, true },
                { Meta.LOG_ITEM, Constants.LOG, false },
                { Meta.LOG_FILE, Constants.LOGFILE, false, new string[] { "Save-SonarrLogFile" } },
                { Meta.QUALITY_DEFINITION, Constants.QUALITY_DEFINITIONS, true },
                { Meta.QUALITY_PROFILE, Constants.PROFILE, true },
                { Meta.RELEASE, Constants.RELEASE, false, new string[] { "Add-SonarrRelease" } },
                { Meta.RELEASE_PROFILE, Constants.RELEASE_PROFILE, true, new string[] { "Get-SonarrTag", "Add-SonarrTag", "Remove-SonarrTag" } },
                { Meta.RENAMABLE, Constants.RENAME, false, new string[] { "Invoke-SonarrRename" } },
                { Meta.ROOT_FOLDER, Constants.ROOTFOLDER, true },
                { Meta.SERIES, Constants.SERIES, true, new string[] { "Get-SonarrEpisode", "Get-SonarrEpisodeFile", "Get-SonarrLanguageProfile", "Get-SonarrQualityProfile", "Get-SonarrRelease", "Get-SonarrTag", "Add-SonarrTag", "Remove-SonarrTag" } },
                { Meta.SERIES_ADD, Constants.SERIES + "/lookup", false, new string[] { "Add-SonarrSeries" } },
                { Meta.TAG, Constants.TAG, true, new string[] { "Remove-SonarrTag" } },
            };

            Debug.Assert(dict.Count <= initialCapacity);
            return services.AddSingleton<IMetadataResolver>(dict);
        }
    }
}
