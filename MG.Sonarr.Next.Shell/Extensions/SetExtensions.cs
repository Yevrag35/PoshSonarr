using MG.Sonarr.Next.Shell.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MG.Sonarr.Next.Shell.Extensions
{
    public static class SetExtensions
    {
        public static bool ValueLike(this IReadOnlySet<WildcardString> set, string value)
        {
            foreach (WildcardString ws in set)
            {
                if (ws.IsMatch(value))
                { 
                    return true;
                }
            }

            return false;
        }
    }
}
