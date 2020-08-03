using MG.Sonarr.Results;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MG.Sonarr
{
    [Obsolete]
    public class EpisodeIdentifierCollection : IEnumerable<EpisodeIdentifier>
    {
        private List<EpisodeIdentifier> _list;

        public EpisodeIdentifier this[int index] => _list[index];
        public int Count => _list.Count;

        [Obsolete]
        private EpisodeIdentifierCollection() => _list = new List<EpisodeIdentifier>();
        public EpisodeIdentifierCollection(IEnumerable<EpisodeIdentifier> identifiers)
        {
            _list = new List<EpisodeIdentifier>(identifiers);
        }

        private void Add(EpisodeIdentifier epId) => _list.Add(epId);
        internal bool AnyMatchesEpisode(EpisodeResult episodeResult)
        {
            bool result = false;
            return _list.Exists(x =>
                x.Seasons.Contains(episodeResult.SeasonNumber.GetValueOrDefault())
                &&
                (
                    x.Episodes.Count <= 0
                    ||
                    x.Episodes.Contains(episodeResult.EpisodeNumber)
                )
            );
            //foreach (EpisodeIdentifier thing in _list)
            //{
            //    if (thing.Season.Overlaps())
            //    //if (thing.Season == episodeResult.SeasonNumber)
            //    //{
            //    //    if (!thing.Episode.HasValue || thing.Episode.GetValueOrDefault() == episodeResult.EpisodeNumber)
            //    //    {
            //    //        result = true;
            //    //        break;
            //    //    }
            //    //}
            //}
            //return result;
            //return _list.Exists(x => x.Season == episodeResult.SeasonNumber &&
            //    (!x.Episode.HasValue || (x.Episode.GetValueOrDefault() == episodeResult.EpisodeNumber)));
        }
        public IEnumerator<EpisodeIdentifier> GetEnumerator() => _list.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();
    }
}
