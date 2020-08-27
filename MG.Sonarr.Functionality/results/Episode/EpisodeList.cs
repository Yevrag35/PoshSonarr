using MG.Sonarr.Functionality;
using MG.Sonarr.Functionality.Collections;
using MG.Sonarr.Functionality.Extensions;
using MG.Sonarr.Results.Collections;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MG.Sonarr.Results
{
    [Serializable]
    public class EpisodeList : SortedListBase<long, ImportEpisode>, IReadOnlyCollection<ImportEpisode>, IReadOnlyList<IEpisode>
    {
        public IEpisode this[int index]
        {
            get
            {
                int posIndex = this.GetPositiveIndex<IEpisode>(index);
                return posIndex > -1 ? base.InnerList[posIndex] : default;
            }
        }

        public IList<long> Ids => base.InnerList.Keys;

        public EpisodeList() : base() { }
        public EpisodeList(IEnumerable<ImportEpisode> episodes)
            : base(episodes, k => k.Id)
        {
        }

        public void Add(IEpisode ep)
        {
            var imep = ImportEpisode.FromOther(ep);
            base.InnerList.Add(ep.Id, imep);
        }
        public void AddRange(IEnumerable<IEpisode> episodes)
        {
            base.AddMultiple(episodes.Select(x => ImportEpisode.FromOther(x)), k => k.Id);
        }
        public void Clear() => base.InnerList.Clear();
        public IEpisode GetById(long id)
        {
            return base.InnerList.ContainsKey(id) ? base.InnerList[id] : null;
        }
        public bool Remove(IEpisode ep)
        {
            return base.InnerList.Remove(ep.Id);
        }
        internal bool TrueForAll(Func<IEpisode, bool> predicate)
        {
            bool result = true;
            foreach (IEpisode iep in base.InnerList.Values)
            {
                if (!predicate(iep))
                {
                    result = false;
                    break;
                }
            }
            return result;
        }

        #region ENUMERATORS
        IEnumerator<IEpisode> IEnumerable<IEpisode>.GetEnumerator()
        {
            foreach (IEpisode iep in base.InnerList.Values)
            {
                yield return iep;
            }
        }

        #endregion
    }
}
