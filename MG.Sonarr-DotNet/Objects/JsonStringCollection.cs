using MG.Sonarr.Results;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace MG.Sonarr
{
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class JsonStringCollection : BaseResult, IEnumerable<string>
    {
        #region FIELDS/CONSTANTS
        private List<string> _list;

        #endregion

        #region INDEXERS
        public string this[int index] => _list[index];

        #endregion

        #region PROPERTIES
        public int Count => _list.Count;

        #endregion

        #region CONSTRUCTORS
        public JsonStringCollection() => _list = new List<string>();
        public JsonStringCollection(int capacity) => _list = new List<string>(capacity);
        internal JsonStringCollection(IEnumerable<string> ignoredTerms) => _list = new List<string>(ignoredTerms);

        #endregion

        #region PUBLIC METHODS
        public void Add(params string[] ignoredTerms)
        {
            var ieq = new IgnoreCase();
            for (int i = 0; i < ignoredTerms.Length; i++)
            {
                string term = ignoredTerms[i];
                if (!_list.Contains(term, ieq))
                {
                    _list.Add(term);
                }
            }
        }
        public bool Contains(string term) => _list.Contains(term, new IgnoreCase());
        internal bool Contains(JToken token) => token != null
            ? _list.Contains(token.ToObject<string>(), new IgnoreCase())
            : false;
        public JToken GetAsToken(int index) => JToken.FromObject(_list[index]);
        public JToken GetAsToken(string term) => JToken.FromObject(_list.Find(x => x.Equals(term)));
        public IEnumerator<string> GetEnumerator() => _list.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();
        public void Remove(params string[] ignoredTerms) => _list.RemoveAll(x => ignoredTerms.Contains(x));
        internal bool Remove(JToken token) => token != null
            ? _list.Remove(token.ToObject<string>())
            : false;

        public override string ToJson() => string.Join(",", this);

        #endregion

        #region CASTS/OPERATORS
        public static implicit operator JsonStringCollection(string[] ignoredTerms) => new JsonStringCollection(ignoredTerms);
        public static implicit operator JsonStringCollection(List<string> listOfStrings) => new JsonStringCollection(listOfStrings);
        public static implicit operator JsonStringCollection(Collection<string> colStrs) => new JsonStringCollection(colStrs);

        #endregion

        private class IgnoreCase : IEqualityComparer<string>
        {
            public bool Equals(string x, string y) => x.Equals(y, StringComparison.CurrentCultureIgnoreCase);
            public int GetHashCode(string x) => x.GetHashCode();
        }
    }
}