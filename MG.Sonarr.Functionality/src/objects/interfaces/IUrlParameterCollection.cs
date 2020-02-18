using System;
using System.Collections.Generic;
using System.Text;

namespace MG.Sonarr.Functionality
{
    public interface IUrlParameterCollection : IList<IUrlParameter>
    {
        void AddRange(IEnumerable<IUrlParameter> items);
        string ToQueryString();
    }
}
