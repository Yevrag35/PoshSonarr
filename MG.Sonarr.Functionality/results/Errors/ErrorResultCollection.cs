using MG.Sonarr.Results.Collections;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace MG.Sonarr.Results
{
    public class ErrorResultCollection : ResultCollectionBase<ErrorResultException>, IList<ErrorResultException>
    {
        public ErrorResultException this[int index]
        {
            get => base.InnerList[index];
            set => base.InnerList[index] = value;
        }

        public bool IsReadOnly => ((ICollection<ErrorResultException>)base.InnerList).IsReadOnly;

        public ErrorResultCollection() : base() { }
        public ErrorResultCollection(IEnumerable<ErrorResultException> errorResults) : base(errorResults) { }

        public void Add(ErrorResultException item) => base.InnerList.Add(item);
        public void Clear() => ((IList<ErrorResultException>)base.InnerList).Clear();
        void ICollection<ErrorResultException>.CopyTo(ErrorResultException[] array, int arrayIndex) => base.InnerList.CopyTo(array, arrayIndex);
        public void Insert(int index, ErrorResultException item) => base.InnerList.Insert(index, item);
        public bool Remove(ErrorResultException item) => base.InnerList.Remove(item);
        public void RemoveAt(int index) => base.InnerList.RemoveAt(index);
    }
}
