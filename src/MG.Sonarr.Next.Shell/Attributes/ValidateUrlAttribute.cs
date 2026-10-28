using MG.Sonarr.Next.Buffers;
using MG.Sonarr.Next.Extensions.Reflection;

namespace MG.Sonarr.Next.Shell.Attributes;

public sealed class ValidateUrlAttribute : ValidateArgumentsAttribute
{
	public UriKind MustBeKind { get; }

	public ValidateUrlAttribute(UriKind mustBeKind)
	{
		this.MustBeKind = mustBeKind;
	}

	protected override void Validate(object arguments, EngineIntrinsics engineIntrinsics)
	{
		if (arguments is Uri goodUrl)
		{
			ValidateUrl(goodUrl, this.MustBeKind);
			return;
		}
		else if (arguments is string possibleUrl && Uri.TryCreate(possibleUrl, UriKind.RelativeOrAbsolute, out Uri? result))
		{
			ValidateUrl(result, this.MustBeKind);
			return;
		}

		ThrowIncorrectType(arguments);
	}

	private static void ValidateUrl(Uri parsedUri, UriKind uriKind)
	{
		bool correctType;
		switch (uriKind)
		{
			case UriKind.RelativeOrAbsolute:
				correctType = true;
				break;

			case UriKind.Absolute:
				correctType = parsedUri.IsAbsoluteUri;
				break;

			case UriKind.Relative:
				correctType = !parsedUri.IsAbsoluteUri;
				break;

			default:
				goto case UriKind.RelativeOrAbsolute;
		}

		if (!correctType)
		{
			ThrowIncorrectUrlKind(in uriKind);
		}
	}

	[DoesNotReturn]
	private static void ThrowIncorrectUrlKind(in UriKind specifiedKind)
	{
		throw new ValidationMetadataException($"An invalid type of URI was passed as an argument. It must be {specifiedKind}.");
	}

	[DoesNotReturn]
	private static void ThrowIncorrectType(object arguments)
	{
		string typeName = arguments?.GetType().GetName() ?? "null";
		throw new ValidationMetadataException($"The parameter argument is NOT of 1 of 2 types: 'string' or 'System.Uri'. Instead, we got '{typeName}'.");
	}

	/// <summary>
	/// Returns a string similar to this attribute's declaration.
	/// </summary>
	/// <remarks>
	///     An example:
	///     <code>[ValidateUrl(UriKind.Absolute)]</code>
	/// </remarks>
	/// <returns></returns>
	public override string ToString()
	{
		const int max_stackalloc = 256;

		ReadOnlySpan<char> name = nameof(ValidateUrlAttribute);
		ReadOnlySpan<char> uriKind = nameof(UriKind);
		ReadOnlySpan<char> value = this.MustBeKind.ToString();
		int index = name.LastIndexOf(nameof(Attribute), StringComparison.InvariantCulture);
		name = name.Slice(0, index);

		// 5 = the number of constant characters beside the names and value.
		// i.e. - '[', '(', '.', ')', ']'
		int length = name.Length + uriKind.Length + value.Length + 5;
		using var builder = SpanStringBuilder.Create(
			length <= max_stackalloc
				? stackalloc char[length]
				: length);

		builder.Append('[');

		builder.Append(name);
		builder.Append('(');
		builder.Append(uriKind);

		builder.Append('.');
		builder.Append(value);
		builder.Append(")]");

		return builder.ToString();
	}
}
