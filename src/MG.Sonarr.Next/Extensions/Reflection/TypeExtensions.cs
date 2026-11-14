namespace MG.Sonarr.Next.Extensions.Reflection;

/// <summary>
/// Custom extensions for the <see cref="Type"/> class.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class TypeExtensions
{
	/// <summary>
	/// Provides extension methods that name a <see cref="Type"/> that isn't <see langword="null"/>.
	/// </summary>
	/// <param name="type">The type whose name is returned.</param>
	extension(Type type)
	{
		/// <summary>
		/// Returns the <see cref="Type"/> class's name for display or logging purposes.
		/// </summary>
		/// <remarks>
		///     A type that may be <see langword="null"/> goes through
		///     <see cref="extension(Type?).GetNameOrNull()"/> or
		///     <see cref="extension(Type?).GetNameOr(string?)"/> instead.
		/// </remarks>
		/// <returns>
		///     By default, <see cref="Type.FullName"/>, or, if <see langword="null"/>,
		///     <see cref="MemberInfo.Name"/>.
		/// </returns>
		internal string GetName()
		{
			return type.FullName ?? type.Name;
		}
	}

	/// <summary>
	/// Provides extension methods that name a <see cref="Type"/> that may be <see langword="null"/>.
	/// </summary>
	/// <param name="type">The type whose name will be returned, or <see langword="null"/>.</param>
	extension(Type? type)
	{
		/// <summary>
		/// Returns the fully qualified name or the member name of the current <see cref="Type"/> -or-
		/// <paramref name="otherValue"/> if the Type instance is <see langword="null"/>.
		/// </summary>
		/// <param name="otherValue">The value to return when <paramref name="type"/> is <see langword="null"/>.</param>
		/// <returns>
		/// The <see cref="Type.FullName"/> of the current <see cref="Type"/> if it is not <see langword="null"/>;
		/// otherwise, the <see cref="MemberInfo.Name"/> instead.  If the type being extended is <see langword="null"/>,
		/// then <paramref name="otherValue"/> is returned.
		/// </returns>
		[DebuggerStepThrough]
		[return: NotNullIfNotNull(nameof(otherValue))]
		internal string? GetNameOr(string? otherValue)
		{
			return type is not null
				? type.GetName()
				: otherValue;
		}
		/// <summary>
		/// Returns the fully qualified name or the member name of the current <see cref="Type"/>.
		/// </summary>
		/// <returns>
		/// The <see cref="Type.FullName"/> of the current <see cref="Type"/> if it is not <see langword="null"/>;
		/// otherwise, the <see cref="MemberInfo.Name"/> instead.  If the type being extended is <see langword="null"/>,
		/// then <see langword="null"/> is returned.
		/// </returns>
		[DebuggerStepThrough]
		[return: NotNullIfNotNull(nameof(type))]
		internal string? GetNameOrNull()
		{
			return type.GetNameOr(otherValue: null);
		}
	}
}
