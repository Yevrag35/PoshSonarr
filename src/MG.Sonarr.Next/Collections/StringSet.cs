namespace MG.Sonarr.Next.Collections
{
    public sealed class StringSet : SortedSet<string>
    {
        public StringSet()
            : base(StringComparer.InvariantCultureIgnoreCase)
        {
        }
    }
}

