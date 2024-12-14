namespace MG.Sonarr.Next.Services.Http.Queries;

public interface IQueryField : ISpanFormattable
{
    string Key { get; }
    int MaxLength { get; }
}
