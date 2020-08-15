using System;
using System.Collections.Generic;
using System.Text;

namespace MG.Sonarr.Functionality.Url
{
    public class SeriesIdParameter : IUrlParameter
    {
        public const string Key = "seriesId";
        private string _value;

        public int Length => 1 + Key.Length + _value.Length;

        public SeriesIdParameter(int seriesId) => _value = Convert.ToString(seriesId);

        public string AsString() => string.Format("{0}={1}", Key, _value);
        public bool Equals(IUrlParameter other)
        {
            if (other is SeriesIdParameter sip)
                return _value.Equals(sip._value);

            else
                return false;
        }
    }
}
