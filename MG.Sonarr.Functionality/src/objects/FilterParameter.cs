﻿using System;
using System.Collections.Generic;
using System.Text;

namespace MG.Sonarr.Functionality
{
    public class FilterParameter : IUrlParameter
    {
        public IConvertible Key { get; set; }
        public IConvertible Value { get; set; }

        public FilterParameter(IConvertible key, IConvertible value)
        {
            this.Key = key;
            this.Value = value;
        }

        public string AsString()
        {
            return string.Format("filterKey={0}&filterValue={1}", Convert.ToString(this.Key), Convert.ToString(this.Value));
        }
    }
}
