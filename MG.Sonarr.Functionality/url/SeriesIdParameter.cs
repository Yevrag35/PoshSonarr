using System;
using System.Collections.Generic;
using System.Text;

namespace MG.Sonarr.Functionality.Url
{
    public class SeriesIdParameter : IUrlParameter
    {
        //IConvertible IUrlParameter.Key => Key;
        public const string Key = "seriesId";

        public int Length => 1 + Key.Length + this.Value.Length;

        //IConvertible IUrlParameter.Value => this.Value;
        public string Value { get; }

        public SeriesIdParameter(int seriesId) => this.Value = Convert.ToString(seriesId);

        public string AsString() => string.Format("{0}={1}", Key, this.Value);
    }
}
