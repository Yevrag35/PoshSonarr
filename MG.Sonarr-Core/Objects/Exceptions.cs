using System;

namespace MG.Sonarr
{
    public class SonarrContextNotSetException : InvalidOperationException
    {
        private protected const string defMsg = "The Sonarr context is not set!{0}.  Run the \"Connect-SonarrInstance\" cmdlet first.";

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
