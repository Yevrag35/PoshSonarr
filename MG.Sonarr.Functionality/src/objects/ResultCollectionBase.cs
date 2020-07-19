using MG.Sonarr.Functionality;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace MG.Sonarr.Results
{
    public abstract class ResultCollectionBase<T> : BaseResult, IEnumerable<T>
    {
        #region FIELDS/CONSTANTS
        private protected List<T> InnerList { get; }

        #endregion

        #region INDEXERS
        //public virtual T this[int index] => this.InnerList[index];

        #endregion

        #region PROPERTIES
        public int Count => this.InnerList.Count;

        #endregion

        #region CONSTRUCTORS
        public ResultCollectionBase() => this.InnerList = new List<T>();
        public ResultCollectionBase(int capacity) => this.InnerList = new List<T>(capacity);
        public ResultCollectionBase(IEnumerable<T> items)
        {
            if (items == null)
                this.InnerList = new List<T>();
            
            else
                this.InnerList = new List<T>(items);
        }

        #endregion

        #region ENUMERATORS
        public IEnumerator<T> GetEnumerator() => this.InnerList.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => this.InnerList.GetEnumerator();

        #endregion

        #region METHODS
        public virtual bool Contains(T item) => this.InnerList.Contains(item);
        internal bool Contains(Predicate<T> match) => this.InnerList.Exists(match);
        internal T Find(Predicate<T> match) => this.InnerList.Find(match);
        internal List<T> FindAll(Predicate<T> match) => this.InnerList.FindAll(match);
        public int IndexOf(T item) => this.InnerList.IndexOf(item);
        internal bool TrueForAll(Predicate<T> match) => this.InnerList.TrueForAll(match);

        private protected string WildcardStringToRegex(string wildcardString)
        {
            return string.Format(
                "^{0}$", 
                Regex.Escape(wildcardString)
                    .Replace("\\*", ".*")
                    .Replace("\\?", "."));
        }

        #endregion
    }
}
