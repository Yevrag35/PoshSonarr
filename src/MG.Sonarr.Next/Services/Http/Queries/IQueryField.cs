namespace MG.Sonarr.Next.Services.Http.Queries;

/// <summary>
/// Provides the key and the length budget of one field in a URL query string.
/// </summary>
/// <remarks>
/// A field formats itself, through <see cref="ISpanFormattable"/>, as the text it contributes to the
/// query string, such as <c>key=value</c>, without the <c>?</c> or <c>&amp;</c> that separates it
/// from the other fields.
/// </remarks>
public interface IQueryField : ISpanFormattable
{
	/// <summary>
	/// Gets the key that identifies the field in a query string.
	/// </summary>
	/// <value>The field's key.</value>
	string Key { get; }
	/// <summary>
	/// Gets the largest number of characters that the field writes when formatted.
	/// </summary>
	/// <value>The maximum number of characters that formatting the field produces.</value>
	int MaxLength { get; }
}
