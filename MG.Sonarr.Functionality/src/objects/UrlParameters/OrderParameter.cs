using System;
using System.Collections.Generic;
using System.Text;

namespace MG.Sonarr.Functionality
{
    public class OrderParameter : IUrlParameter
    {
        public IConvertible Key => "order";

        public IConvertible Value => throw new NotImplementedException();

        public string AsString() => Key + "=" + Value;
    }
}
