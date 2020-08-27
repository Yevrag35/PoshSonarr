using MG.Sonarr.Functionality.Extensions;
using MG.Sonarr.Results.Collections;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace MG.Sonarr.Results
{
    public class ErrorResultCollection : ResultCollectionBase<ErrorResultException>, IList<ErrorResultException>, IReadOnlyList<ErrorResultException>
    {
        public ErrorResultException this[int index]
        {
            get
            {
                int posIndex = this.GetPositiveIndex(index);
                return posIndex > -1 ? base.InnerList[posIndex] : null;
            }
            set
            {
                int posIndex = this.GetPositiveIndex(index);
                if (posIndex > -1)
                    base.InnerList[posIndex] = value;
            }
        }

        bool ICollection<ErrorResultException>.IsReadOnly => false;

        internal ErrorResultCollection() : base() { }
        [JsonConstructor]
        internal ErrorResultCollection(IEnumerable<ErrorResultException> errorResults) : base(errorResults) { }

        public void Add(ErrorResultException item) => base.InnerList.Add(item);
        public void Clear() => base.InnerList.Clear();
        void ICollection<ErrorResultException>.CopyTo(ErrorResultException[] array, int arrayIndex) => base.InnerList.CopyTo(array, arrayIndex);
        public void Insert(int index, ErrorResultException item) => base.InnerList.Insert(index, item);
        public bool Remove(ErrorResultException item) => base.InnerList.Remove(item);
        public void RemoveAt(int index) => base.InnerList.RemoveAt(index);
    }
}
