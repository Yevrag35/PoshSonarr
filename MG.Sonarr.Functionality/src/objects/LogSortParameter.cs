using System;
using System.Collections.Generic;
using System.Text;

namespace MG.Sonarr.Functionality
{
    public class LogSortParameter : IUrlParameter
    {
        IConvertible IUrlParameter.Key => this.Key;
        public LogSortKey Key { get; set; }

        IConvertible IUrlParameter.Value => this.Value;
        public SortDirection Value { get; set; }

        public LogSortParameter(LogSortKey sortKey, SortDirection direction)
        {
            this.Key = sortKey;
            this.Value = direction;
        }

        public string AsString()
        {
            string dir = "desc";
            if (this.Value == SortDirection.Ascending)
                dir = "asc";

            return string.Format("sortKey={0}&sortDir={1}", this.Key.ToString().ToLower(), dir);
        }
    }
}
