using MG.Sonarr.Functionality;
using MG.Sonarr.Results.Collections;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MG.Sonarr.Results
{
    [Serializable]
    public class EpisodeList : IEnumerable<ImportEpisode>
    {
        private SortedList<long, ImportEpisode> _list;

        public IEpisode this[int index] => _list.Values[index];

        public EpisodeList()
        {
            _list = new SortedList<long, ImportEpisode>();
        }
        public EpisodeList(IEnumerable<ImportEpisode> episodes)
        {
            if (episodes is ICollection<ImportEpisode> icol)
                _list = new SortedList<long, ImportEpisode>(icol.Count);

            else
                _list = new SortedList<long, ImportEpisode>();

            foreach (ImportEpisode iep in episodes)
            {
                _list.Add(iep.Id, iep);
            }
        }

        public void Add(IEpisode ep)
        {
            var imep = ImportEpisode.FromOther(ep);
            _list.Add(ep.Id, imep);
        }
        public void AddRange(IEnumerable<IEpisode> episodes)
        {
            foreach (IEpisode iep in episodes)
            {
                var imEp = ImportEpisode.FromOther(iep);
                _list.Add(iep.Id, imEp);
            }
        }
        public void Clear() => _list.Clear();
        public IEpisode GetByID(long id)
        {
            if (_list.ContainsKey(id))
                return _list[id];

            else
                return null;
        }
        public bool Remove(ImportEpisode ep) => _list.Remove(ep.Id);

        public IEnumerator<ImportEpisode> GetEnumerator()
        {
            foreach (ImportEpisode imep in _list.Values.OfType<ImportEpisode>())
            {
                yield return imep;
            }
        }
        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        public bool TrueForAll(Func<IEpisode, bool> predicate)
        {
            bool result = true;
            foreach (IEpisode iep in _list.Values)
            {
                if (!predicate(iep))
                {
                    result = false;
                    break;
                }
            }
            return result;
        }
    }
}
