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
    /// <summary>
    /// A <see cref="string"/> collection class that when serialized into JSON joins the 
    /// collection elements by comma (no spaces).
    /// </summary>
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class JsonStringCollection : BaseResult, IEnumerable<string>
    {
        #region FIELDS/CONSTANTS
        private List<string> _list;

        #endregion

        #region INDEXERS
        /// <summary>
        /// Retrieves the specified <see cref="string"/> element by its index from within the collection.
        /// </summary>
        public string this[int index] => _list[index];

        #endregion

        #region PROPERTIES
        /// <summary>
        /// Gets the number of elements contained within the <see cref="JsonStringCollection"/>.
        /// </summary>
        public int Count => _list.Count;

        #endregion

        #region CONSTRUCTORS
        /// <summary>
        /// Initializes a new instance of the <see cref="JsonStringCollection"/> class
        /// that is empty and has the default initial capacity.
        /// </summary>
        public JsonStringCollection() => _list = new List<string>();
        /// <summary>
        /// Initializes a new instance of the <see cref="JsonStringCollection"/> class
        /// that is empty and has the specified initial capacity.
        /// </summary>
        /// <param name="capacity">The number of elements that the new collection can initially store.</param>
        public JsonStringCollection(int capacity) => _list = new List<string>(capacity);
        /// <summary>
        /// Initializes a new instance of the <see cref="JsonStringCollection"/> class that contains elements
        /// copied from the specified collection and has sufficient capacity to accommodate the number of 
        /// elements copied.
        /// </summary>
        /// <param name="ignoredTerms">The generic collection of <see cref="string"/> whose elements are copied to the new collection.</param>
        public JsonStringCollection(IEnumerable<string> ignoredTerms) => _list = new List<string>(ignoredTerms);

        #endregion

        #region PUBLIC METHODS
        /// <summary>
        /// Adds one or more <see cref="string"/> to the end of the collection.
        /// </summary>
        /// <param name="ignoredTerms">The string(s) to be added to the end of the collection.</param>
        public void Add(params string[] ignoredTerms)
        {
            if (ignoredTerms == null)
                throw new ArgumentNullException("ignoredTerms");

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

        internal void Add(object obj)
        {
            if (obj is string str)
                _list.Add(str);

            else if (obj is string[] strs)
                _list.AddRange(strs);
        }

        /// <summary>
        /// Removes all strings from the collection.
        /// </summary>
        public void Clear() => _list.Clear();

        /// <summary>
        /// Determines whether the collection contains a specified <see cref="string"/> using a 
        /// case-insensitive comparer.
        /// </summary>
        /// <param name="term">The <see cref="string"/> to locate in the collection.</param>
        /// <returns></returns>
        public bool Contains(string term) => _list.Contains(term, new IgnoreCase());

        internal bool Contains(JToken token) => token != null
            ? _list.Contains(token.ToObject<string>(), new IgnoreCase())
            : false;

        /// <summary>
        /// Retrieves the specified <see cref="string"/> by its index and converts it to a <see cref="JToken"/>.
        /// </summary>
        /// <param name="index">The index of the <see cref="string"/> to locate.</param>
        /// <returns></returns>
        public JToken GetAsToken(int index) => JToken.FromObject(_list[index]);
        /// <summary>
        /// Retrieves the specified <see cref="string"/> and converts it to a <see cref="JToken"/>.
        /// </summary>
        /// <param name="term">The <see cref="string"/> to locate within the collection.</param>
        /// <returns></returns>
        public JToken GetAsToken(string term) => JToken.FromObject(_list.Find(x => x.Equals(term)));
        /// <summary>
        /// Returns a <see cref="string"/> enumerator that iterates through the <see cref="JsonStringCollection"/>.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<string> GetEnumerator() => _list.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();

        /// <summary>
        /// Merges the contents of a given <see cref="JsonStringCollection"/> into this collection.  If a <see cref="string"/> 
        /// is missing from this collection, it is added.  If a <see cref="string"/> is present in this collection but missing
        /// from the specified collection, it is removed.
        /// </summary>
        /// <param name="jsonCol">The <see cref="JsonStringCollection"/> to compare.</param>
        public void MergeCollections(JsonStringCollection jsonCol)
        {
            if (!jsonCol.ToJson().Equals(this.ToJson()))
            {
                for (int i = 0; i < jsonCol.Count; i++)
                {
                    string str = jsonCol[i];
                    if (!this.Contains(str))
                        _list.Add(str);
                }

                for (int r = _list.Count - 1; r >= 0; r--)
                {
                    string rStr = _list[r];
                    if (!jsonCol.Contains(rStr))
                        _list.Remove(rStr);
                }
            }
        }

        /// <summary>
        /// Removes one or more <see cref="string"/> from the collection.
        /// </summary>
        /// <param name="ignoredTerms"></param>
        public void Remove(params string[] ignoredTerms) => _list.RemoveAll(x => ignoredTerms.Contains(x));

        internal bool Remove(JToken token) => token != null
            ? _list.Remove(token.ToObject<string>())
            : false;

        /// <summary>
        /// Overrides the <see cref="BaseResult.ToJson()"/> method to join each element by comma and no spaces.
        /// </summary>
        /// <returns></returns>
        public override string ToJson() => string.Join(",", this);

        #endregion

        #region CASTS/OPERATORS
        public static implicit operator JsonStringCollection(string[] ignoredTerms) => new JsonStringCollection(ignoredTerms);
        public static implicit operator JsonStringCollection(List<string> listOfStrings) => new JsonStringCollection(listOfStrings);
        public static implicit operator JsonStringCollection(Collection<string> colStrs) => new JsonStringCollection(colStrs);

        #endregion
    }
}