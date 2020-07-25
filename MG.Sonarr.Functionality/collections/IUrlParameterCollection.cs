using System;
using System.Collections.Generic;
using System.Text;

namespace MG.Sonarr.Functionality.Collections
{
    public interface IUrlParameterCollection : IList<IUrlParameter>
    {
        void AddRange(IEnumerable<IUrlParameter> items);
        string ToQueryString();
    }
}
