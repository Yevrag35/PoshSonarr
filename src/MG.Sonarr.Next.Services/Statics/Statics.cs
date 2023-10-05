using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace MG.Sonarr.Next.Services
{
    public static class Statics
    {
        public static readonly CultureInfo DefaultCulture = CultureInfo.CurrentCulture;
        public static readonly IFormatProvider DefaultProvider = DefaultCulture;
    }
}