using MG.Sonarr.Next.Buffers;
using System.Management.Automation;

namespace MG.Sonarr.Next.PSProperties;

public sealed class ReadOnlyTagsProperty : ReadOnlyProperty<SortedSet<int>>
{
	public SortedSet<int> Tags { get; internal set; }
	protected override SortedSet<int> ValueAsT => this.Tags;

	public ReadOnlyTagsProperty(SortedSet<int> set)
		: base(Constants.TAGS)
	{
		this.Tags = set;
	}

	public override PSMemberInfo Copy()
	{
		return new ReadOnlyTagsProperty([.. this.Tags]);
	}

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

