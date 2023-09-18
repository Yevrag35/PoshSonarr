using MG.Sonarr.Next.Services.Extensions;
using MG.Sonarr.Next.Services.Http;
using MG.Sonarr.Next.Services.Json;
using MG.Sonarr.Next.Shell.Cmdlets;
using Microsoft.Extensions.DependencyInjection;
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

        
        public static void WriteSonarrResult<T>(this Cmdlet cmdlet, SonarrResponse<T> result)
        {
            if (result.IsError)
            {
                cmdlet.WriteError(result.Error);
            }
            else if (!result.IsEmpty)
            {
                cmdlet.WriteObject(result.Data);
            }
        }
    }
}