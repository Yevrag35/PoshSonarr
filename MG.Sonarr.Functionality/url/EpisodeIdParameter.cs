using System;
using System.Collections.Generic;
using System.Text;

namespace MG.Sonarr.Functionality.Url
{
    public class EpisodeIdParameter : IUrlParameter
    {
        IConvertible IUrlParameter.Key => this.Key;
        public string Key => "episodeId";

        IConvertible IUrlParameter.Value => this.Value;
        public long Value { get; set; }

        public EpisodeIdParameter(long episodeId) => this.Value = episodeId;

        public string AsString()
        {
            return string.Format("{0}={1}", this.Key, this.Value);
        }
    }
}
