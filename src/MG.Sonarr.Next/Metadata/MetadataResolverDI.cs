using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions.Strings;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Reflection;

namespace MG.Sonarr.Next.Metadata
{
    public static class MetadataResolverDI
    {
        public static IServiceCollection AddMetadata(this IServiceCollection services, Assembly cmdletAssembly)
        {
            int initialCapacity = 35;
            var pipes = FindPipeableCmdlets(cmdletAssembly);
            MetadataResolver dict = new(initialCapacity, pipes)
            {
                { Meta.BACKUP, Constants.BACKUP, true },
                { Meta.CALENDAR, Constants.CALENDAR, false },
                { Meta.COMMAND, Constants.COMMAND, true },
                { Meta.DELAY_PROFILE, Constants.DELAY_PROFILE, true },
                { Meta.DISK, Constants.DISKSPACE, false },
                { Meta.DOWNLOAD_CLIENT, Constants.DOWNLOAD_CLIENT, true },
                { Meta.DOWNLOAD_CLIENT_CONFIG, Constants.DOWNLOAD_CLIENT_CONFIG, true },
                { Meta.DOWNLOAD_CLIENT_SCHEMA, Constants.DOWNLOAD_CLIENT_SCHEMA, false },
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
                { Meta.REMOTE_PATH_MAPPING, Constants.REMOTE_PATH, true },
                { Meta.RENAMABLE, Constants.RENAME, false },
                { Meta.REVISION, Constants.QUALITY_DEFINITIONS, false },
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

        internal static Dictionary<string, ImmutableArray<string>> FindPipeableCmdlets(Assembly cmdletAssembly)
        {
            IEnumerable<Type> cmdletTypes = cmdletAssembly.GetExportedTypes()
                .Where(x => x.IsClass
                         && !x.IsAbstract
                         && x.IsDefined(typeof(CmdletAttribute), inherit: false)
                         && x.IsDefined(typeof(MetadataCanPipeAttribute), inherit: false));

            return cmdletTypes
                .SelectMany(type =>
                {
                    string cmdletName = GetCmdletNameFromAttribute(type);
                    return type.GetCustomAttributes<MetadataCanPipeAttribute>()
                               .Select(att => new TwoStrings(att.Tag, cmdletName));
                })
                .GroupBy(x => x.First)
                .ToDictionary(
                    keySelector: x => x.Key,
                    elementSelector: x => x.Select(c => c.Second).Order(StringComparer.Ordinal).ToImmutableArray(),
                    comparer: StringComparer.OrdinalIgnoreCase);
        }

        [StructLayout(LayoutKind.Auto)]
        private readonly struct TwoStrings
        {
            public readonly string First;
            public readonly string Second;
            internal TwoStrings(string first, string second)
            {
                First = first ?? string.Empty;
                Second = second ?? string.Empty;
            }
        }

        private static string GetCmdletNameFromAttribute(Type cmdletType)
        {
            TwoStrings verbAndNoun = GetVerbAndNoun(cmdletType);
            return string.Concat(verbAndNoun.First, ['-'], verbAndNoun.Second);
        }

        /// <summary>
        /// Extracts the verb and noun components from the CmdletAttribute applied to the specified cmdlet type.
        /// </summary>
        /// <param name="cmdletType">The type representing the cmdlet from which to retrieve the verb and noun. Must have a CmdletAttribute with
        /// a constructor that accepts two string arguments.</param>
        /// <returns>
        /// A struct instance containing the verb and noun specified in the CmdletAttribute of the given cmdlet type.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// <paramref name="cmdletType"/> does not have a <see cref="CmdletAttribute"/> with a constructor that accepts two string arguments.
        /// </exception>
        private static TwoStrings GetVerbAndNoun(Type cmdletType)
        {
            foreach (CustomAttributeData cad in cmdletType.CustomAttributes)
            {
                if (typeof(CmdletAttribute).Equals(cad.AttributeType) && TryGetVerbAndNoun(cad.ConstructorArguments, out string? verb, out string? noun))
                {
                    return new(verb, noun);
                }
            }

            throw new ArgumentException($"{cmdletType} does not have the right CmdletAttribute constructor signature.", nameof(cmdletType));
        }

        private static bool TryGetVerbAndNoun(
            IList<CustomAttributeTypedArgument> constructorArgs,
            [NotNullWhen(true)] out string? verb,
            [NotNullWhen(true)] out string? noun)
        {
            if (constructorArgs.Count < 2
                || constructorArgs[0].Value is not string v
                || constructorArgs[1].Value is not string n)
            {
                verb = null;
                noun = null;
                return false;
            }

            verb = v;
            noun = n;
            return true;
        }
    }
}
