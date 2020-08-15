using MG.Sonarr.Functionality.Url;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MG.Sonarr.Functionality.Collections
{
    public class UrlParameterCollection2 : IEnumerable<IUrlParameter>//, IEnumerable<KeyValuePair<IUrlParameter, (int, int)>>
    {
        #region PRIVATE FIELDS/CONSTANTS

        //private List<IUrlParameter> _list;
        private Dictionary<IUrlParameter, (int, int)> _params;
        public StringBuilder Builder;
        private const string SEPARATOR = "&";
        private const string START_FILTER = "?";

        #endregion

        #region INDEXER
        //public IUrlParameter this[int index] 
        //{
        //    get => _list[index]; 
        //    set => _list[index] = value; 
        //}

        #endregion

        #region PROPERTIES
        public int Count => _params.Count;
        //bool ICollection<IUrlParameter>.IsReadOnly => ((ICollection<IUrlParameter>)_list).IsReadOnly;
        public int Length => 1 + _params.Keys.Sum(x => x.Length);

        #endregion

        #region CONSTRUCTORS
        public UrlParameterCollection2()
        {
            Builder = new StringBuilder();
            _params = new Dictionary<IUrlParameter, (int, int)>(new ParameterEquality());
        }
        //public UrlParameterCollection2(int capacity) => _list = new List<IUrlParameter>(capacity);
        //public UrlParameterCollection2(IEnumerable<IUrlParameter> items) => _list = new List<IUrlParameter>(items.Where(x => x != null));

        #endregion

        #region CUSTOM EQUALITY
        private class ParameterEquality : IEqualityComparer<IUrlParameter>
        {
            public bool Equals(IUrlParameter x, IUrlParameter y)
            {
                if (x is SortParameter && y is SortParameter)
                {
                    return true;
                }
                else if (x != null && y != null)
                    return x.GetType().Equals(y.GetType());

                else
                    return false;
            }
            public int GetHashCode(IUrlParameter parameter) => parameter.GetHashCode();
        }

        #endregion

        #region COLLECTION METHODS
        public void Add(params IUrlParameter[] items)
        {
            if (items == null || items.Length <= 0)
                return;

            for (int i = 0; i < items.Length; i++)
            {
                IUrlParameter param = items[i];

                if (!_params.ContainsKey(param))
                    this.PrivateAdd(param);
            }
        }
        public void Clear() => _params.Clear();
        public bool Contains(IUrlParameter item) => _params.ContainsKey(item);
        //void ICollection<IUrlParameter>.CopyTo(IUrlParameter[] array, int arrayIndex) => _list.CopyTo(array, arrayIndex);
        public IEnumerator<IUrlParameter> GetEnumerator() => _params.Keys.GetEnumerator();
        //IEnumerator<KeyValuePair<IUrlParameter, (int, int)>> IEnumerable<KeyValuePair<IUrlParameter, (int, int)>>.GetEnumerator() => _params.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _params.Keys.GetEnumerator();
        private void PrivateAdd(IUrlParameter parameter)
        {
            int lastPos = 0;
            if (_params.Count > 0)
                lastPos = _params.Values.Max(x => x.Item2);

            int newEndIndex = lastPos + parameter.Length;
            //Builder.Insert(lastPos, parameter.AsString());
            _params.Add(parameter, (this.Count, lastPos));
        }
        public bool Remove(IUrlParameter item) => _params.Remove(item);

        #endregion

        #region SPECIAL METHODS
        //public void AddPagingParameter(int pageNumber, int pageSize)
        //{
        //    var newParam = PagingParameter.Create(pageNumber, pageSize);
        //    IUrlParameter existing = _list.Find(x => x is PagingParameter);
        //    if (existing != null)
        //    {
        //        if (!newParam.Equals(existing))
        //            _list.Remove(existing);

        //        else
        //            return;
        //    }

        //    this.PrivateAdd(newParam);
        //}
        //public void AddSortParameter(SortParameter parameter)
        //{
        //    IUrlParameter existing = _list.Find(x => x is SortParameter);
        //    if (existing != null)
        //    {
        //        if (!parameter.Equals(existing))
        //            _list.Remove(existing);

        //        else
        //            return;
        //    }

        //    this.PrivateAdd(parameter);
        //}
        /// <summary>
        /// Returns whether the <see cref="UrlParameterCollection"/> contains any elements of the specific type.
        /// </summary>
        /// <typeparam name="T">The .NET type to search for that implements <see cref="IUrlParameter"/>.</typeparam>
        /// <returns>
        ///     <see langword="true"/>: if the collection contains a <see cref="IUrlParameter"/> of type <typeparamref name="T"/>;
        ///     otherwise, <see langword="false"/>.
        /// </returns>
        public bool ContainsType<T>() where T : IUrlParameter
        {
            return _params.Keys.OfType<T>().Any();
        }
        public static void ApplyOneOffs(ref StringBuilder newBuilder, params IUrlParameter[] oneOffs)
        {
            if (oneOffs != null && oneOffs.Length > 0)
            {
                if (newBuilder.Length > 0)
                    newBuilder.Append(SEPARATOR);

                for (int n = 0; n < oneOffs.Length; n++)
                {
                    newBuilder.Append(oneOffs[n].AsString());

                    if (n < oneOffs.Length - 1)
                        newBuilder.Append(SEPARATOR);
                }
            }
        }
        public string ToQueryString(params IUrlParameter[] oneOffs)
        {
            Builder.Clear();
            int oneOffLength = this.Length + (oneOffs?.Sum(x => x.Length)).GetValueOrDefault();
            Builder.EnsureCapacity(this.Length + oneOffLength);

            foreach (var kvp in _params.OrderBy(x => x.Value.Item1))
            {
                Builder.Append(kvp.Key.AsString());

                if (kvp.Value.Item1 < this.Count - 1)
                    Builder.Append(SEPARATOR);
            }
            //for (int i = 0; i < this.Count; i++)
            //{
            //    Builder.Append(this[i].AsString());

            //    if (i < this.Count - 1)
            //        Builder.Append(SEPARATOR);
            //}

            if (oneOffLength > 0)
                ApplyOneOffs(ref Builder, oneOffs);

            Builder.Insert(0, START_FILTER);
            return Builder.ToString();
        }

        #endregion
    }
}
