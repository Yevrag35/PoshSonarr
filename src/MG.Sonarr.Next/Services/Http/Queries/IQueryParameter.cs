namespace MG.Sonarr.Next.Services.Http.Queries
{
    public interface IQueryParameter : IEquatable<IQueryParameter>, IEquatable<QueryParameter>, ISpanFormattable
    {
        string Key { get; }
        int MaxLength { get; }
    }
}
