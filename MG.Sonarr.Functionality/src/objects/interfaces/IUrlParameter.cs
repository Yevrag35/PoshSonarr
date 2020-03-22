using System;
using System.Collections.Generic;
using System.Text;

namespace MG.Sonarr.Functionality
{
    public interface IUrlParameter
    {
        IConvertible Key { get; }
        IConvertible Value { get; }

        string AsString();
    }
}
