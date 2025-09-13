using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MG.Sonarr.Next.Enums;

internal static class ForEachFlagExtensions
{
    internal static FlagEnumerator<T> Enumerate<T>(this T mask) where T : unmanaged, Enum
    {
        return new FlagEnumerator<T>(mask);
    }
}
