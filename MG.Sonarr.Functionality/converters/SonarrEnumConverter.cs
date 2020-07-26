using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System;

namespace MG.Sonarr.Functionality.Converters
{
    public class SonarrStringEnumConverter : StringEnumConverter
    {
        public SonarrStringEnumConverter() : base(new CamelCaseNamingStrategy())
        {
        }
    }
}
