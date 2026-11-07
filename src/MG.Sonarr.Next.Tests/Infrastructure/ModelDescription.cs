namespace MG.Sonarr.Next.Tests.Infrastructure;

/// <summary>
/// Describes what PowerShell shows of an object, so a test can tell whether a model looks the same after a request
/// that sends it as before.
/// </summary>
internal static class ModelDescription
{
	/// <summary>
	/// Describes what PowerShell shows of an object: its type names, and each property's name, kind, whether it's
	/// settable, and value, with nested objects and collections described in turn.
	/// </summary>
	/// <remarks>
	/// The properties are described in order of name, since their order isn't part of what a model keeps: deserialization
	/// already replaces some of them, which moves them to the end, and the client then adds the tag.
	/// </remarks>
	/// <param name="pso">The object to describe.</param>
	/// <returns>The description, one line for each property and each item of a collection.</returns>
	internal static string Describe(PSObject pso)
	{
		StringBuilder builder = new();
		Describe(pso, builder, indent: 0);
		return builder.ToString();
	}

	private static void Describe(PSObject pso, StringBuilder builder, int indent)
	{
		string pad = new(' ', indent * 2);
		_ = builder.Append(pad).AppendJoin(", ", pso.TypeNames).AppendLine();

		foreach (PSPropertyInfo property in pso.Properties.OrderBy(p => p.Name, StringComparer.Ordinal))
		{
			_ = builder.Append(pad)
				.Append(property.Name)
				.Append(" (").Append(property.GetType().Name)
				.Append(property.IsSettable ? ", settable): " : "): ");

			DescribeValue(property.Value, builder, indent + 1);
		}
	}

	private static void DescribeValue(object? value, StringBuilder builder, int indent)
	{
		switch (value)
		{
			case null:
				_ = builder.AppendLine("null");
				break;

			case PSObject nested when nested.BaseObject is PSCustomObject:
				_ = builder.AppendLine();
				Describe(nested, builder, indent);
				break;

			case string text:
				_ = builder.Append('"').Append(text).AppendLine("\"");
				break;

			case IEnumerable items:
				_ = builder.AppendLine("[");
				foreach (object? item in items)
				{
					_ = builder.Append(' ', indent * 2);
					DescribeValue(item, builder, indent + 1);
				}

				_ = builder.Append(' ', indent * 2).AppendLine("]");
				break;

			default:
				_ = builder.AppendLine(string.Create(CultureInfo.InvariantCulture, $"{value} ({value.GetType().Name})"));
				break;
		}
	}
}
