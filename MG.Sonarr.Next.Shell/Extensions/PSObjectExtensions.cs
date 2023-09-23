using MG.Sonarr.Next.Services.Metadata;
using MG.Sonarr.Next.Shell.Components;
using Namotion.Reflection;
using PSO = MG.Sonarr.Next.Services.Extensions.PSO.PSOExtensions;

namespace MG.Sonarr.Next.Shell.Extensions
{
    public static class PSObjectExtensions
    {
        internal static void AddMetadata(this object? obj, MetadataTag? tag)
        {
            if (obj is PSObject pso)
            {
                AddMetadata(pso: pso, tag);
            }
        }
        internal static void AddMetadata(this PSObject pso, MetadataTag? tag)
        {
            if (tag is not null)
            {
                pso.Properties.Add(new MetadataProperty(tag));
            }
        }
        
        public static void AddNameAlias(this object? obj)
        {
            if (obj is PSObject pso)
            {
                AddNameAlias(pso: pso);
            }
        }
        public static void AddNameAlias(this PSObject pso)
        {
            pso.Properties.Add(new PSAliasProperty(Constants.NAME, Constants.TITLE));
        }
        internal static bool IsCorrectType(this object? obj, [ConstantExpected] string tag, [NotNullWhen(true)] out PSObject? pso)
        {
            return PSO.IsCorrectType(obj, Constants.META_PROPERTY_NAME, tag, out pso);
        }

        public static bool PropertyLike(this object? obj, string propertyName, string value)
        {
            if (PSO.TryGetProperty(obj, propertyName, out string? strVal))
            {
                WildcardString ws = value;
                return ws.IsMatch(strVal);
            }

            return false;
        }
    }
}
