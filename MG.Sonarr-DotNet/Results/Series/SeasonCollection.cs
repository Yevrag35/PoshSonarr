using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MG.Sonarr.Results
{
    public class SeasonCollection : BaseResult, IEnumerable<Season>
    {
        #region FIELDS/CONSTANTS
        private List<Season> _list;

        #endregion

        #region INDEXERS
        public Season this[int index] => _list[index];

        #endregion

        #region PROPERTIES
        public int Count => _list.Count;

        #endregion

        #region CONSTRUCTORS
        internal SeasonCollection() => _list = new List<Season>();
        internal SeasonCollection(int capacity) => _list = new List<Season>(capacity);
        internal SeasonCollection(IEnumerable<Season> seasons) => _list = new List<Season>(seasons);

        #endregion

        #region PUBLIC METHODS
        public IEnumerator<Season> GetEnumerator() => ((IEnumerable<Season>)_list).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<Season>)_list).GetEnumerator();

        #endregion

        #region BACKEND/PRIVATE METHODS
        internal void Add(Season season) => _list.Add(season);

        #endregion
    }
}