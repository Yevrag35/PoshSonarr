namespace MG.Sonarr.Next.Exceptions
{
    public sealed class EmptyHttpResponseException : PoshSonarrException
    {
        public string Url { get; }

        public EmptyHttpResponseException(string url)
            : base("An empty response was received when there should have been one.")
        {
            this.Url = url;
        }
    }
}
