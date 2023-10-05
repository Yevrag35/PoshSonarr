using System;

namespace MG.Sonarr.Functionality.Url
{
    public class OrderParameter : IUrlParameter
    {
        public IConvertible Key => "order";

        public IConvertible Value => throw new NotImplementedException();

        public string AsString() => Key + "=" + Value;
    }
}
