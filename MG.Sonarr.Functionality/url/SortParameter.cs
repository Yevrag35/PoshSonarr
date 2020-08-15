using System;
using System.Collections.Generic;
using System.Text;

namespace MG.Sonarr.Functionality.Url
{
    public abstract class SortParameter
    {
        protected string SortDirectionString { get; private set; }

        /// <summary>
        /// The direction the key is sorted.
        /// </summary>
        public SortDirection Value
        {
            get => (SortDirection)Enum.Parse(typeof(SortDirection), this.SortDirectionString);
            set => this.SortDirectionString = this.GetSortString(value);
        }

        public SortParameter(SortDirection direction) => this.Value = direction;

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
