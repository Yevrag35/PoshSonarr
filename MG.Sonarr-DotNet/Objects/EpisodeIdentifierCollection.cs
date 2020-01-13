using MG.Sonarr.Results;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MG.Sonarr
{
    public class EpisodeIdentifierCollection : IEnumerable<EpisodeIdentifier>
    {
        private List<EpisodeIdentifier> _list;

        public EpisodeIdentifier this[int index] => _list[index];
        public int Count => _list.Count;

        private EpisodeIdentifierCollection() => _list = new List<EpisodeIdentifier>();

        private void Add(EpisodeIdentifier epId) => _list.Add(epId);
        internal bool AnyMatchesEpisode(EpisodeResult episodeResult)
        {
            return _list.Exists(x => x.Season == episodeResult.SeasonNumber &&
                (!x.Episode.HasValue || (x.Episode.HasValue && x.Episode.Value == episodeResult.EpisodeNumber)));
        }
        public IEnumerator<EpisodeIdentifier> GetEnumerator() => _list.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();

        public static explicit operator EpisodeIdentifierCollection(string possibleIdentifier)
        {
            if (string.IsNullOrEmpty(possibleIdentifier))
                return null;

            EpisodeIdentifier epId = possibleIdentifier;
            var col = new EpisodeIdentifierCollection
            {
                epId
            };
            return col;
        }
        public static implicit operator EpisodeIdentifierCollection(string[] possibleIdentifiers)
        {
            if (possibleIdentifiers == null)
                return null;

            var col = new EpisodeIdentifierCollection();

            foreach (string str in possibleIdentifiers)
            {
                EpisodeIdentifier epId = str;
                col.Add(epId);
            }
            return col;
        }
        //public static implicit operator EpisodeIdentifierCollection(object[] possibleIdentifiers)
        //{
        //    if (possibleIdentifiers == null)
        //        return null;

        //    var col = new EpisodeIdentifierCollection();

        //    foreach (string str in possibleIdentifiers)
        //    {
        //        EpisodeIdentifier epId = str;
        //        col.Add(epId);
        //    }
        //    return col;
        //}
    }
}
