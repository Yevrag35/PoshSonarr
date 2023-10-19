namespace MG.Sonarr.Next.Exceptions
{
    public sealed class ReadOnlyPropertyException : PoshSonarrException
    {
        const string MSG = "Unable to set the property value because '{0}' is marked as read-only.";

        public ReadOnlyPropertyException(string propertyName)
            : base(string.Format(MSG, propertyName))
        {
        }
    }
}
