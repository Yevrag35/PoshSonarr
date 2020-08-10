using System;
using System.Collections.Generic;
using System.Text;

namespace MG.Sonarr.Functionality.Url
{
    public abstract class SortParameter
    {
        /// <summary>
        /// The direction the key is sorted.
        /// </summary>
        public SortDirection Value { get; set; }

        public SortParameter(SortDirection direction) => this.Value = direction;

        protected virtual string GetSortString()
        {
            switch (this.Value)
            {
                case SortDirection.Ascending:
                    return "asc";

                default:
                    return "desc";
            }
        }
    }
}
