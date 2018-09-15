using System;

namespace Sonarr
{
    public class SonarrContextNotSetException : NotImplementedException
    {
        private protected const string defMsg = "The Sonarr context is not set!{0}";

        public SonarrContextNotSetException()
            : base(string.Format(defMsg, string.Empty))
        {
        }

        public SonarrContextNotSetException(string additionalMsg)
            : base(string.Format(defMsg, additionalMsg))
        {
        }
    }
}
