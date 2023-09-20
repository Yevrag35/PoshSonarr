using MG.Sonarr.Next.Shell.Components;
using Namotion.Reflection;
using PSO = MG.Sonarr.Next.Services.Extensions.PSOExtensions;

namespace MG.Sonarr.Next.Shell.Extensions
{
    public static class PSObjectExtensions
    {
        internal static void AddMetadataTag(this object? obj, [ConstantExpected] string tag)
        {
            if (obj is PSObject pso)
            {
                AddMetadataTag(pso: pso, tag);
            }
        }
        internal static void AddMetadataTag(this PSObject pso, [ConstantExpected] string tag)
        {
            PSO.AddProperty(pso, Constants.META_PROPERTY_NAME, tag);
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
