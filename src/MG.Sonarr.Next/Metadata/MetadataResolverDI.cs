using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Collections;
using Microsoft.Extensions.DependencyInjection;
using System.Management.Automation;
using System.Reflection;

namespace MG.Sonarr.Next.Metadata
{
    public static class MetadataResolverDI
    {
        public static IServiceCollection AddMetadata(this IServiceCollection services, Assembly cmdletAssembly)
        {
            int initialCapacity = 32;
            NameLookup<string> pipes = FindPipeableCmdlets(cmdletAssembly);
            MetadataResolver dict = new(initialCapacity, pipes)
            {
                { Meta.BACKUP, Constants.BACKUP, true },
                { Meta.CALENDAR, Constants.CALENDAR, false },
                { Meta.COMMAND, Constants.COMMAND, true },
                { Meta.DELAY_PROFILE, Constants.DELAY_PROFILE, true },
                { Meta.DISK, Constants.DISKSPACE, false },
                { Meta.DOWNLOAD_CLIENT, Constants.DOWNLOAD_CLIENT, true },
                { Meta.DOWNLOAD_CLIENT_CONFIG, Constants.DOWNLOAD_CLIENT_CONFIG, true },
                { Meta.EPISODE, Constants.EPISODE, true },
                { Meta.EPISODE_FILE, Constants.EPISODEFILE, true },
                { Meta.HISTORY, Constants.HISTORY, false },
                { Meta.HISTORY_SINCE, Constants.HISTORY_SINCE, false },
                { Meta.HOST, Constants.HOST, true },
                { Meta.INDEXER, Constants.INDEXER, true },
                { Meta.LANGUAGE, Constants.LANGUAGE_PROFILE, true },
                { Meta.LOG_ITEM, Constants.LOG, false },
                { Meta.LOG_FILE, Constants.LOGFILE, false },
                { Meta.MANUAL_IMPORT, Constants.MANUAL_IMPORT, false },
                { Meta.MEDIA_MANGEMENT, Constants.MEDIA_MANAGEMENT, true },
                { Meta.NAMING_CONFIG, Constants.NAMING_CONFIG, true },
                { Meta.NOTIFICATION, Constants.NOTIFICATION, true },
                { Meta.QUALITY, Constants.QUALITY_DEFINITIONS, false },
                { Meta.QUALITY_DEFINITION, Constants.QUALITY_DEFINITIONS, true },
                { Meta.QUALITY_PROFILE, Constants.PROFILE, true },
                { Meta.RELEASE, Constants.RELEASE, false },
                { Meta.RELEASE_PROFILE, Constants.RELEASE_PROFILE, true },
                { Meta.RENAMABLE, Constants.RENAME, false },
                { Meta.ROOT_FOLDER, Constants.ROOTFOLDER, true },
                { Meta.SERIES, Constants.SERIES, true },
                { Meta.SERIES_ADD, Constants.SERIES_LOOKUP, false },
                { Meta.SERIES_HISTORY, Constants.HISTORY_BY_SERIES, false },
                { Meta.STATUS, Constants.STATUS, false },
                { Meta.TAG, Constants.TAG, true },
            };

            Debug.Assert(dict.Count <= initialCapacity);
            return services.AddSingleton<IMetadataResolver>(dict);
        }

        internal static NameLookup<string> FindPipeableCmdlets(Assembly cmdletAssembly)
        {
            StringComparer comparer = StringComparer.InvariantCultureIgnoreCase;
            NameLookup<string> namesToTags = new(100, comparer, comparer);

            Type cmdletAtt = typeof(CmdletAttribute);
            Type pipeAtt = typeof(MetadataCanPipeAttribute);

            IEnumerable<Type> cmdletTypes = cmdletAssembly.GetExportedTypes()
                .Where(x => x.IsDefined(cmdletAtt, false) && x.IsDefined(pipeAtt, false));

            foreach (Type cmdlet in cmdletTypes)
            {
                string cmdletName = GetCmdletNameFromAttribute(cmdlet);
                foreach (MetadataCanPipeAttribute mta in cmdlet.GetCustomAttributes<MetadataCanPipeAttribute>())
                {
                    namesToTags.Add(mta.Tag, cmdletName);
                }
            }

            return namesToTags;
        }

        private static string GetCmdletNameFromAttribute(Type cmdlet)
        {
            CmdletAttribute ca = cmdlet.GetCustomAttribute<CmdletAttribute>() ?? throw new InvalidOperationException();
            return string.Create(ca.VerbName.Length + ca.NounName.Length + 1, ca, (chars, state) =>
            {
                state.VerbName.CopyTo(chars);

                int position = state.VerbName.Length;
                chars[position++] = '-';

                state.NounName.CopyTo(chars.Slice(position));
            });
        }
    }
}
