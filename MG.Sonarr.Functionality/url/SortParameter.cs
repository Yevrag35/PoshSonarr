using System;
using System.Collections.Generic;
using System.Text;

namespace MG.Sonarr.Functionality.Url
{
    public abstract class SortParameter : IUrlParameter
    {
        private const string SORT_KEY_FORMAT = "sortKey={0}";
        private const string SORT_DIR_FORMAT = "sortDir={0}";

        protected StringBuilder Builder;
        int IUrlParameter.Length => this.GetLength();

        protected SortParameter(SortDirection direction)
        {
            Builder = new StringBuilder(string.Format(SORT_DIR_FORMAT, this.GetSortString(direction)));
        }

        protected void AddSortKey(string key)
        {
            Builder.Insert(0, string.Format(SORT_KEY_FORMAT, key));
        }
        string IUrlParameter.AsString() => this.FormatAsString();
        protected virtual string FormatAsString()
        {
            return Builder.ToString();
        }
        public virtual int GetLength() => Builder.Length;
        private string GetSortString(SortDirection direction)
        {
            switch (direction)
            {
                case SortDirection.Ascending:
                    return "asc";

                default:
                    return "desc";
            }
        }
    }
}
