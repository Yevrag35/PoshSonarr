using MG.Sonarr.Functionality;
using MG.Sonarr.Functionality.Collections;
using MG.Sonarr.Results.Collections;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace MG.Sonarr.Results.Collections
{
    public class EpisodeCollection : ResultListBase<EpisodeResult>, IReadOnlyList<IRecord>
    {
        IRecord IReadOnlyList<IRecord>.this[int index] => this[index];

        [JsonConstructor]
        internal EpisodeCollection(IEnumerable<EpisodeResult> episodes)
            : base(episodes) { }

        IEnumerator<IRecord> IEnumerable<IRecord>.GetEnumerator() => this.GetEnumerator();
    }
}
