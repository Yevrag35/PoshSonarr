namespace MG.Sonarr.Next.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class MetadataCanPipeAttribute : Attribute
    {
        public required string Tag { get; init; }
    }
}
