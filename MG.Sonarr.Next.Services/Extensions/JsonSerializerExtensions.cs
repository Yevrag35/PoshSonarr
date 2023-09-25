﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MG.Sonarr.Next.Services.Extensions
{
    public static class JsonSerializerExtensions
    {
        public static string ConvertName(this JsonSerializerOptions options, string name)
        {
            return options.PropertyNamingPolicy?.ConvertName(name) ?? name;
        }
    }
}