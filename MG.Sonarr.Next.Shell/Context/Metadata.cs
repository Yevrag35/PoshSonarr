using MG.Sonarr.Next.Services.Metadata;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MG.Sonarr.Next.Shell.Context
{
    internal static class MetadataHandler
    {
        internal static void AddMetadata(IServiceCollection services)
        {
            MetadataResolver dict = new(10)
            {
                { Meta.CALENDAR, Constants.CALENDAR, false, Meta.SERIES, Meta.EPISODE },
                { Meta.EPISODE, Constants.EPISODE, true, Meta.SERIES, Meta.EPISODE_FILE },
                { Meta.EPISODE_FILE, Constants.EPISODEFILE, true, Meta.EPISODE },
                { Meta.INDEXER, Constants.INDEXER, true },
                { Meta.ROOT_FOLDER, Constants.ROOTFOLDER, true },
                { Meta.SERIES, Constants.SERIES, true, Meta.TAG, Meta.EPISODE },
                { Meta.SERIES_ADD, Constants.SERIES + "/lookup", false },
                { Meta.TAG, Constants.TAG, true },
            };
            
            services.AddSingleton(dict);
        }
    }
}
