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
                { Meta.EPISODE, "/episode", true },
                { Meta.EPISODEFILE, "/episodefile", true },
                { Meta.SERIES, "/series", true },
                { Meta.TAG, "/tag", true },
            };
            
            services.AddSingleton(dict);
        }
    }
}
