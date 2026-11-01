namespace MG.Sonarr.Next.Extensions.Reflection;

/// <summary>
/// Custom extensions for the <see cref="Type"/> class.
/// </summary>
public static class TypeExtensions
{
	/// <summary>
	/// Returns the <see cref="Type"/> class's name for display or logging purposes.
	/// </summary>
	/// <remarks>
	///     A type that may be <see langword="null"/> goes through <see cref="GetNameOrNull(Type?)"/> or
	///     <see cref="GetNameOr(Type?, string?)"/> instead.
	/// </remarks>
	/// <param name="type">The type whose name is returned.</param>
	/// <returns>
	///     By default, <see cref="Type.FullName"/>, or, if <see langword="null"/>,
	///     <see cref="MemberInfo.Name"/>.
	/// </returns>
	public static string GetName(this Type type)
	{
		return type.FullName ?? type.Name;
	}
	/// <summary>
	/// Returns the fully qualified name or the member name of the current <see cref="Type"/> -or-
	/// <paramref name="otherValue"/> if the Type instance is <see langword="null"/>.
	/// </summary>
	/// <param name="type">The type whose name will be returned.</param>
	/// <param name="otherValue">The value to return when <paramref name="type"/> is <see langword="null"/>.</param>
	/// <returns>
	/// The <see cref="Type.FullName"/> of the current <see cref="Type"/> if it is not <see langword="null"/>;
	/// otherwise, the <see cref="MemberInfo.Name"/> instead.  If the type being extended is <see langword="null"/>,
	/// then <paramref name="otherValue"/> is returned.
	/// </returns>
	[DebuggerStepThrough]
	[return: NotNullIfNotNull(nameof(otherValue))]
	public static string? GetNameOr(this Type? type, string? otherValue)
	{
		return type is not null
			? type.GetName()
			: otherValue;
	}
	/// <summary>
	/// Returns the fully qualified name or the member name of the current <see cref="Type"/>.
	/// </summary>
	/// <param name="type">The type whose name will be returned.</param>
	/// <returns>
	/// The <see cref="Type.FullName"/> of the current <see cref="Type"/> if it is not <see langword="null"/>;
	/// otherwise, the <see cref="MemberInfo.Name"/> instead.  If the type being extended is <see langword="null"/>,
	/// then <see langword="null"/> is returned.
	/// </returns>
	[DebuggerStepThrough]
	[return: NotNullIfNotNull(nameof(type))]
	public static string? GetNameOrNull(this Type? type)
	{
		return type.GetNameOr(otherValue: null);
	}
}
