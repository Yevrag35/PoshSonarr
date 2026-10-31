using MG.Sonarr.Next.Buffers;
using System.Management.Automation;

namespace MG.Sonarr.Next.PSProperties;

/// <summary>
/// Represents the read-only <c>Tags</c> note property of a Sonarr object, which holds the IDs of the object's tags.
/// </summary>
/// <remarks>
/// PowerShell can't replace the set, but code can change it, and a change shows in PowerShell and in the next request
/// body that the object is written to.
/// </remarks>
public sealed class ReadOnlyTagsProperty : ReadOnlyProperty<SortedSet<int>>
{
	/// <summary>
	/// Gets the set of tag IDs that the property holds.
	/// </summary>
	/// <value>The property's value.</value>
	public SortedSet<int> Tags { get; internal set; }
	/// <summary>
	/// Gets the property's value as a <see cref="SortedSet{T}"/>.
	/// </summary>
	/// <value>The value of <see cref="Tags"/>.</value>
	protected override SortedSet<int> ValueAsT => this.Tags;

	/// <summary>
	/// Initializes a new <see cref="ReadOnlyTagsProperty"/> instance, named <c>Tags</c>, that holds the specified set.
	/// </summary>
	/// <param name="set">The set of tag IDs that the property holds.</param>
	public ReadOnlyTagsProperty(SortedSet<int> set)
		: base(Constants.TAGS)
	{
		this.Tags = set;
	}

	/// <summary>
	/// Creates a copy of the property that holds a copy of its set.
	/// </summary>
	/// <returns>A new <see cref="ReadOnlyTagsProperty"/> that holds a new set with the same tag IDs.</returns>
	public override PSMemberInfo Copy()
	{
		return new ReadOnlyTagsProperty([.. this.Tags]);
	}

	/// <summary>
	/// Returns a description of the property, with its tag IDs.
	/// </summary>
	/// <returns>
	/// The text <c>read-only SortedSet[int] Tags={&lt;IDs&gt;}</c>, with the IDs separated by commas, such as
	/// <c>read-only SortedSet[int] Tags={1,4}</c>.
	/// </returns>
	public override string ToString()
	{
		const string AS_STRING = "read-only SortedSet[int] Tags=";
		const string AS_EMPTY = AS_STRING + "{}";
		const int MAX_STACKALLOC = 256;

		if (this.Tags.Count == 0)
		{
			return AS_EMPTY;
		}

		int length = AS_STRING.Length + 2
			+
			(this.Tags.Count * LengthConstants.INT_MAX)
			+
			(this.Tags.Count - 1);

		using SpanStringBuilder builder = SpanStringBuilder.Create(
			length <= MAX_STACKALLOC
				? stackalloc char[length]
				: length);

		builder.Append(AS_STRING);
		builder.Append('{');

		foreach (int id in this.Tags)
		{
			builder.Append(id, LengthConstants.INT_MAX);
			builder.Append(',');
		}

		builder.BackspaceIf(',');

		builder.Append('}');

		return builder.ToString();
	}
}

