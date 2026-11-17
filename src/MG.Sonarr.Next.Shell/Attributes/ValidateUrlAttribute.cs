using MG.Sonarr.Next.Buffers;
using MG.Sonarr.Next.Extensions.Reflection;

namespace MG.Sonarr.Next.Shell.Attributes;

/// <summary>
/// Provides the validation of a URL argument as an absolute or a relative URL.
/// </summary>
/// <remarks>
/// The argument must be a <see cref="Uri"/>, or a string that parses as one, and of the kind that
/// <see cref="MustBeKind"/> names. <see cref="UriKind.RelativeOrAbsolute"/>, or an undefined kind, takes either.
/// </remarks>
public sealed class ValidateUrlAttribute : ValidateArgumentsAttribute
{
	/// <summary>
	/// Gets the kind of URL that the argument must be.
	/// </summary>
	/// <value><see cref="UriKind.Absolute"/> or <see cref="UriKind.Relative"/> for that kind alone, or
	/// <see cref="UriKind.RelativeOrAbsolute"/> for either.</value>
	public UriKind MustBeKind { get; }

	/// <summary>
	/// Initializes a new <see cref="ValidateUrlAttribute"/> instance with the specified kind of URL.
	/// </summary>
	/// <param name="mustBeKind">The kind of URL that the argument must be.</param>
	public ValidateUrlAttribute(UriKind mustBeKind)
	{
		this.MustBeKind = mustBeKind;
	}

	/// <summary>
	/// Checks that the argument is a URL of the kind that <see cref="MustBeKind"/> names.
	/// </summary>
	/// <param name="arguments">The argument: a <see cref="Uri"/>, or a string that parses as one.</param>
	/// <param name="engineIntrinsics">The engine APIs of the session that binds the argument, which the method doesn't
	/// read.</param>
	/// <exception cref="ValidationMetadataException">Thrown when the argument is neither a <see cref="Uri"/> nor a string
	/// that parses as one, or is a URL of the wrong kind.</exception>
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

	/// <summary>
	/// Checks that a URL is of the specified kind.
	/// </summary>
	/// <param name="parsedUri">The URL.</param>
	/// <param name="uriKind">The kind that the URL must be. <see cref="UriKind.RelativeOrAbsolute"/>, or an undefined
	/// kind, takes either.</param>
	/// <exception cref="ValidationMetadataException">Thrown when <paramref name="parsedUri"/> isn't of
	/// <paramref name="uriKind"/>.</exception>
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

	/// <summary>
	/// Fails validation for a URL of the wrong kind.
	/// </summary>
	/// <param name="specifiedKind">The kind that the URL must be, which the message names.</param>
	/// <exception cref="ValidationMetadataException">Always thrown.</exception>
	[DoesNotReturn]
	private static void ThrowIncorrectUrlKind(in UriKind specifiedKind)
	{
		throw new ValidationMetadataException($"An invalid type of URI was passed as an argument. It must be {specifiedKind}.");
	}

	/// <summary>
	/// Fails validation for an argument that is neither a <see cref="Uri"/> nor a string that parses as one.
	/// </summary>
	/// <param name="arguments">The argument, whose type the message names, or <see langword="null"/>.</param>
	/// <exception cref="ValidationMetadataException">Always thrown.</exception>
	[DoesNotReturn]
	private static void ThrowIncorrectType(object arguments)
	{
		string typeName = arguments?.GetType().GetName() ?? "null";
		throw new ValidationMetadataException($"The parameter argument is NOT of 1 of 2 types: 'string' or 'System.Uri'. Instead, we got '{typeName}'.");
	}

	/// <summary>
	/// Returns the attribute's declaration as C# writes it, such as <c>[ValidateUrl(UriKind.Absolute)]</c>.
	/// </summary>
	/// <returns>The attribute's name without its <c>Attribute</c> suffix, followed by <see cref="MustBeKind"/> as a
	/// <see cref="UriKind"/> member, in brackets.</returns>
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
