using System;
using System.Collections.Generic;
using System.Text;

namespace MG.Sonarr.Functionality.Url
{
    public struct EpisodeIdParameter : IUrlParameter
    {
        private string _value;

        //IConvertible IUrlParameter.Key => Key;
        public const string Key = "episodeId";
        public int Length => 1 + Key.Length + _value.Length;
        //IConvertible IUrlParameter.Value => this.Value;
        public long Value
        {
            get => Convert.ToInt64(_value);
            set => _value = Convert.ToString(value);
        }

        public EpisodeIdParameter(long episodeId)
        {
            _value = Convert.ToString(episodeId);

        }

        public string AsString()
        {
            return string.Format("{0}={1}", Key, _value);
        }

        public bool Equals(IUrlParameter other)
        {
            return other is EpisodeIdParameter epParam && _value.Equals(epParam._value) ? true : false;
        }
    }
}
