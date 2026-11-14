namespace MG.Sonarr.Next.Strings;

public readonly partial struct Wildcard
{
	/// <summary>
	/// Returns the underlying <see cref="string"/> instance of this <see cref="Wildcard"/> object; no
	/// actual conversion is performed.
	/// </summary>
	/// <returns>The underlying <see cref="string"/>.</returns>
	[DebuggerStepThrough]
	public override string ToString()
	{
		return !this.IsEmpty ? _pattern : string.Empty;
	}
}