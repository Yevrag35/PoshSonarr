using System;
using System.Collections.Generic;
using System.Text;

namespace MG.Sonarr.Functionality.Url
{
    public struct EpisodeIdParameter : IUrlParameter
    {
        private long _value;

        IConvertible IUrlParameter.Key => Key;
        public const string Key = "episodeId";

        IConvertible IUrlParameter.Value => this.Value;
        public long Value
        {
            get => _value;
            set => _value = value;
        }

        public EpisodeIdParameter(long episodeId) => _value = episodeId;

        public string AsString()
        {
            return string.Format("{0}={1}", Key, this.Value);
        }
    }
}
