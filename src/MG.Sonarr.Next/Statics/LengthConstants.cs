namespace MG.Sonarr.Next;

/// <summary>
/// Provides constant values for various length constraints.
/// </summary>
public static class LengthConstants
{
	/// <summary>
	/// Maximum length for a byte value.
	/// </summary>
	public const int BYTE_MAX = 3;

	/// <summary>
	/// Maximum length for an integer value.
	/// </summary>
	public const int INT_MAX = 11;

	/// <summary>
	/// Maximum length for a long value.
	/// </summary>
	public const int LONG_MAX = 20;

	/// <summary>
	/// Maximum length for an unsigned integer value.
	/// </summary>
	public const int UINT_MAX = INT_MAX - 1;

	/// <summary>
	/// Maximum length for an unsigned long value.
	/// </summary>
	/// <value><c>20</c></value>
	public const int ULONG_MAX = LONG_MAX;

	/// <summary>
	/// Represents the maximum number of characters for floating-point values.
	/// </summary>
	// Worst case is a 9-significant-digit value in exponential form with a sign, e.g. "-1.00000055E+20" (15).
	// float.MinValue ("-3.4028235E+38") is only 14 chars because it carries 8 significant digits, not 9.
	public const int FLOAT_MAX = 15;

	/// <summary>
	/// Maximum length for a double value.
	/// </summary>
	public const int DOUBLE_MAX = 24;    // double.MinValue.ToString().Length

	/// <summary>
	/// Maximum length for a decimal value.
	/// </summary>
	// Worst case is a full-precision value carrying a decimal point with a sign, e.g.
	// "-7.9228162514264337593543950335" (31). decimal.MinValue ("-79228162514264337593543950335")
	// is only 30 chars because it has no fractional part.
	public const int DECIMAL_MAX = 31;

	/// <summary>
	/// Maximum length for a 128-bit integer value.
	/// </summary>
	public const int INT128_MAX = 40;

	/// <summary>
	/// Length of a GUID in 'B' or 'P' format.
	/// </summary>
	public const int GUID_FORM_B_OR_P = 38;

	/// <summary>
	/// Length of a GUID in 'N' format.
	/// </summary>
	public const int GUID_FORM_N = 32;

	/// <summary>
	/// Length of a GUID in 'D' format.
	/// </summary>
	public const int GUID_FORM_D = 36;

	/// <summary>
	/// Length of a GUID in 'X' format.
	/// </summary>
	public const int GUID_FORM_X = 68;

	/// <summary>
	/// Gets the length of a GUID based on the specified format character.
	/// </summary>
	/// <param name="format">The format character ('B', 'P', 'N', 'D', or 'X').</param>
	/// <param name="paramName">The argument name reported in thrown exceptions. Automatically supplied from the caller's <paramref name="format"/> expression.</param>
	/// <returns>The length of the GUID in the specified format.</returns>
	/// <inheritdoc cref="GetGuidLengthCore(char, string?)" path="/exception"/>
	public static int GetGuidLength(char format, [CallerArgumentExpression(nameof(format))] string? paramName = null)
	{
		return GetGuidLengthCore(format, paramName);
	}

	// Intentionally kept out-of-line: inlining this helper substantially bloats hot callers
	// (for example AppendGuid) without producing better downstream codegen.
	/// <summary>
	/// Gets the length of a GUID based on the specified read-only span consisting of a single character.
	/// </summary>
	/// <param name="format">The read-only span that contains the single format character.</param>
	/// <param name="paramName">The argument name reported in thrown exceptions. Automatically supplied from the caller's <paramref name="format"/> expression.</param>
	/// <returns><inheritdoc cref="GetGuidLength(char, string?)"/></returns>
	/// <exception cref="ArgumentException">Thrown when the format is not a single character.</exception>
	/// <inheritdoc cref="GetGuidLengthCore(char, string?)" path="/exception"/>
	[MethodImpl(MethodImplOptions.NoInlining)]
	public static int GetGuidLength(ReadOnlySpan<char> format, [CallerArgumentExpression(nameof(format))] string? paramName = null)
	{
		return format.Length switch
		{
			0 => GUID_FORM_D, // Default format
			1 => GetGuidLengthCore(format[0], paramName),
			_ => throwEx(format, paramName),
		};

		[DoesNotReturn, StackTraceHidden, MethodImpl(MethodImplOptions.NoInlining)]
		static int throwEx(ReadOnlySpan<char> format, string? paramName)
		{
			throw new ArgumentException("GUID formats must be only 1 character in length.", paramName);
		}
	}

	/// <summary>
	/// Gets the length of a GUID based on the specified format character.
	/// </summary>
	/// <param name="format">The format character to evaluate.</param>
	/// <param name="paramName">The argument name reported in thrown exceptions.</param>
	/// <returns>The length of the GUID in the specified format.</returns>
	/// <exception cref="FormatException">Thrown when the format character is invalid.</exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static int GetGuidLengthCore(char format, string? paramName)
	{
		return format switch
		{
			'B' or 'P' or 'b' or 'p' => GUID_FORM_B_OR_P,
			'N' or 'n' => GUID_FORM_N,
			'D' or 'd' => GUID_FORM_D,
			'X' or 'x' => GUID_FORM_X,
			_ => throwFormatEx(format, paramName),
		};

		[DoesNotReturn, StackTraceHidden, MethodImpl(MethodImplOptions.NoInlining)]
		static int throwFormatEx(char format, string? paramName)
		{
			paramName = paramName is not null ? $"{paramName}: " : null;
			throw new FormatException($"{paramName}Invalid GUID format specified -> {format}");
		}
	}
}