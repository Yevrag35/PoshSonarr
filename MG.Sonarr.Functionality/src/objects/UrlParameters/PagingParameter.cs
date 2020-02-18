using System;
using System.Collections.Generic;
using System.Text;

namespace MG.Sonarr.Functionality
{
    public class PagingParameter : IUrlParameter
    {
        public const int DefaultPage = 1;
        public const int DefaultPageSize = 10;
        private int _key;
        private int _val;

        IConvertible IUrlParameter.Key => this.Key;
        public int Key
        {
            get => _key;
            set
            {
                if (value <= 0)
                    _key = DefaultPage;

                else
                    _key = value;
            }
        }
        IConvertible IUrlParameter.Value => this.Value;
        public int Value
        {
            get => _val;
            set
            {
                if (value <= 0)
                    _val = DefaultPageSize;

                else
                    _val = value;
            }
        }

        public PagingParameter(int pageNumber = DefaultPage, int pageSize = DefaultPageSize)
        {
            this.Key = pageNumber;
            this.Value = pageSize;
        }

        public string AsString()
        {
            return string.Format("page={0}&pageSize={1}", this.Key, this.Value);
        }
    }
}
