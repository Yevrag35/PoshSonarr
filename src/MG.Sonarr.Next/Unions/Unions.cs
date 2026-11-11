namespace MG.Sonarr.Next.Unions;

internal readonly union StringOrBool(string, bool);
/// <summary>
/// A union that holds either a value of type <typeparamref name="T"/> or a value of type <see cref="SonarrErrorRecord"/>.
/// </summary>
public readonly union SonarrHttpResult<T>(T, SonarrErrorRecord);