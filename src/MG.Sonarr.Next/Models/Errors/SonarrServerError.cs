using MG.Sonarr.Next.Attributes;
using MG.Sonarr.Next.Extensions.PSO;
using MG.Sonarr.Next.Extensions.Reflection;
using MG.Sonarr.Next.Json;
using MG.Sonarr.Next.Metadata;

namespace MG.Sonarr.Next.Models.Errors;

/// <summary>
/// Represents one error in the body of a Sonarr error response, such as a field that failed validation.
/// </summary>
/// <remarks>
/// <para>
/// An error stands for no endpoint, so it keeps whatever metadata tag it has.
/// </para>
/// <para>
/// Errors sort by <see cref="PropertyName"/> and then by <see cref="Message"/>, ignoring case.
/// </para>
/// </remarks>
[SonarrObject]
public sealed class SonarrServerError : SonarrObject,
	IComparable<SonarrServerError>,
	ISerializableNames<SonarrServerError>
{
	/// <summary>
	/// The number of properties that a new error reserves room for.
	/// </summary>
	const int CAPACITY = 5;
	/// <summary>
	/// The name of the property that holds a problem details object's messages, keyed by the field that each is about.
	/// </summary>
	const string ERRORS = "Errors";
	/// <summary>
	/// The type name of a property whose value is a string.
	/// </summary>
	const string STRING_TYPE = "System.String";
	/// <summary>
	/// The name of the property that holds a problem details object's summary.
	/// </summary>
	const string TITLE = "Title";
	/// <summary>
	/// The full name of the type, which <see cref="SetPSTypeName"/> inserts into the object's type names.
	/// </summary>
	static readonly string _typeName = typeof(SonarrServerError).GetName();

	/// <summary>
	/// Gets the error's message.
	/// </summary>
	/// <remarks>
	/// <para>It's built while the error is deserialized, from the values of the string properties whose names end in
	/// <c>message</c>, ignoring case, such as <c>ErrorMessage</c>.</para>
	/// <para>An error with no such property, such as the problem details that Sonarr answers with when it can't read a
	/// request's body, takes its <c>Title</c> and then each message in its <c>Errors</c>, after the name of the field
	/// that the message is about, such as <c>$.name: The JSON value could not be converted to System.String.</c></para>
	/// </remarks>
	/// <value>
	/// Those values, each on its own line, or an empty string when the error has none of them.
	/// </value>
	public string Message { get; private set; } = string.Empty;
	/// <summary>
	/// Gets the name of the field that the error is about.
	/// </summary>
	/// <remarks>
	/// It's a copy of the <c>PropertyName</c> property, taken while the error is deserialized.
	/// </remarks>
	/// <value>The field's name, or <see langword="null"/> when Sonarr sends none.</value>
	public string? PropertyName { get; private set; }

	/// <summary>
	/// Initializes a new <see cref="SonarrServerError"/> instance with no properties.
	/// </summary>
	public SonarrServerError()
		: base(CAPACITY)
	{
	}

	/// <summary>
	/// Compares this error with another by property name and then by message.
	/// </summary>
	/// <param name="other">The error to compare with, or <see langword="null"/>.</param>
	/// <returns>
	/// A negative number if this error sorts before <paramref name="other"/>, 0 if they sort together, or a positive
	/// number if it sorts after. Property names compare ordinally and messages by the invariant culture, both ignoring
	/// case.
	/// </returns>
	public int CompareTo(SonarrServerError? other)
	{
		int compare = StringComparer.OrdinalIgnoreCase.Compare(this.PropertyName, other?.PropertyName);
		if (compare == 0)
		{
			compare = StringComparer.InvariantCultureIgnoreCase.Compare(this.Message, other?.Message);
		}

		return compare;
	}
	/// <summary>
	/// Gets the metadata tag for an error, which is the tag that it already has.
	/// </summary>
	/// <param name="resolver">The resolver that holds the connection's tags, which an error doesn't use.</param>
	/// <param name="existing">The tag that the object has now.</param>
	/// <returns><paramref name="existing"/>.</returns>
	protected override MetadataTag GetTag(IMetadataResolver resolver, MetadataTag existing)
	{
		return existing;
	}

	/// <summary>
	/// Finishes deserializing the error and builds <see cref="Message"/> and <see cref="PropertyName"/> from its
	/// properties.
	/// </summary>
	public override void OnDeserialized()
	{
		base.OnDeserialized();
		var col = this.Properties.Match("*message");
		if (col.Count > 0)
		{
			var values = col
				.Where(x => x.IsGettable && x.TypeNameOfValue == STRING_TYPE)
				.Select(x => x.Value.ToString());

			this.Message = string.Join(Environment.NewLine, values);
		}

		if (this.Message.Length == 0)
		{
			this.Message = this.BuildProblemDetailsMessage();
		}

		if (this.TryGetNonNullProperty(nameof(this.PropertyName), out string? propName))
		{
			this.PropertyName = propName;
		}
	}
	/// <summary>
	/// Builds a message from the <c>Title</c> and <c>Errors</c> of a problem details object.
	/// </summary>
	/// <returns>
	/// The title, followed by each message in <c>Errors</c> after the name of its field, each on its own line, or an
	/// empty string when the error has neither.
	/// </returns>
	private string BuildProblemDetailsMessage()
	{
		List<string> lines = [];
		if (this.TryGetNonNullProperty(TITLE, out string? title) && !string.IsNullOrWhiteSpace(title))
		{
			lines.Add(title);
		}

		if (this.Properties[ERRORS]?.Value is PSObject errors)
		{
			foreach (PSPropertyInfo field in errors.Properties)
			{
				object? value = field.Value;
				if (value is string single)
				{
					lines.Add(string.Concat(field.Name, ": ", single));
				}
				else if (value is IEnumerable messages)
				{
					foreach (object? message in messages)
					{
						if (message is not null)
						{
							lines.Add(string.Concat(field.Name, ": ", message.ToString()));
						}
					}
				}
			}
		}

		return string.Join(Environment.NewLine, lines);
	}

	/// <summary>
	/// Inserts the error's own type name ahead of the base type names.
	/// </summary>
	protected override void SetPSTypeName()
	{
		base.SetPSTypeName();
		this.TypeNames.Insert(0, _typeName);
	}
}
