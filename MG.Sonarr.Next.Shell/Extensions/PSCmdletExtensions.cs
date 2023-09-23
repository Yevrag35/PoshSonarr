using MG.Sonarr.Next.Services.Extensions;
using MG.Sonarr.Next.Services.Http;
using MG.Sonarr.Next.Services.Json;
using MG.Sonarr.Next.Services.Metadata;
using MG.Sonarr.Next.Shell.Cmdlets;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.DependencyInjection;
using OneOf;
using System.Text.Json;

namespace MG.Sonarr.Next.Shell.Extensions
{
    public static class PSCmdletExtensions
    {
        public static bool HasParameter<T>(this T cmdlet, Expression<Func<T, object?>> parameter) where T : PSCmdlet
        {
            return parameter.TryGetAsMember(out MemberExpression? memEx)
                && cmdlet.MyInvocation.BoundParameters.ContainsKey(memEx.Member.Name);
        }
        
        public static void WriteCollection<T>(this Cmdlet cmdlet, IEnumerable<T> collection)
        {
            cmdlet.WriteObject(collection, enumerateCollection: true);
        }
        public static void WriteCollection<T>(this Cmdlet cmdlet, MetadataTag? tag, IEnumerable<T> values)
            where T : PSObject
        {
            foreach (T value in values)
            {
                WriteObject(cmdlet, tag, value, enumerateCollection: false);
            }
        }
        public static void WriteObject<T>(this Cmdlet cmdlet, MetadataTag? tag, T value, bool enumerateCollection = true) where T : PSObject
        {
            value.AddMetadata(tag);
            cmdlet.WriteObject(value, enumerateCollection);
        }

        public static void WriteSonarrResults<T>(this Cmdlet cmdlet, SonarrResponse<T> result, MetadataTag? tag = null) where T : IEnumerable<PSObject>
        {
            if (result.IsError)
            {
                cmdlet.WriteError(result.Error);
            }
            else if (!result.IsEmpty)
            {
                WriteCollection(cmdlet, tag, result.Data);
            }
        }
        public static void WriteSonarrResult<T>(this Cmdlet cmdlet, SonarrResponse<T> result, MetadataTag? tag = null) where T : PSObject
        {
            if (result.IsError)
            {
                cmdlet.WriteError(result.Error);
            }
            else if (!result.IsEmpty)
            {
                WriteObject(cmdlet, tag, result.Data, enumerateCollection: false);
            }
        }
    }
}