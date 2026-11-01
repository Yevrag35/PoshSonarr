namespace MG.Sonarr.Next.Exceptions;

/// <summary>
/// Represents an exception thrown when attempting to set a read-only property.
/// </summary>
public sealed class ReadOnlyPropertyException : PoshSonarrException
{
	const string MSG = "Unable to set the property value because '{0}' is marked as read-only.";

	/// <summary>
	/// Initializes a new <see cref="ReadOnlyPropertyException"/> instance.
	/// </summary>
	/// <param name="propertyName">The read-only property name.</param>
	public ReadOnlyPropertyException(string propertyName)
		: base(string.Format(CultureInfo.CurrentCulture, MSG, propertyName))
	{
	}
}
