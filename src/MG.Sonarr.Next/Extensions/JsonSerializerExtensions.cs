namespace MG.Sonarr.Next.Extensions;

/// <summary>
/// Provides extension methods for <see cref="JsonSerializerOptions"/>.
/// </summary>
public static class JsonSerializerExtensions
{
	/// <summary>
	/// Converts a property name with the options' naming policy.
	/// </summary>
	/// <param name="options">The options whose <see cref="JsonSerializerOptions.PropertyNamingPolicy"/> converts the name, or <see langword="null"/>.</param>
	/// <param name="name">The name to convert, or <see langword="null"/>.</param>
	/// <returns>
	/// The name that the naming policy converts <paramref name="name"/> to, or <paramref name="name"/> itself when
	/// it's <see langword="null"/>, empty, or white space, or when <paramref name="options"/> is
	/// <see langword="null"/> or has no naming policy.
	/// </returns>
	[return: NotNullIfNotNull(nameof(name))]
	public static string? ConvertName(this JsonSerializerOptions? options, string? name)
	{
		if (string.IsNullOrWhiteSpace(name) || options is null || options.PropertyNamingPolicy is null)
		{
			return name;
		}

		return options.PropertyNamingPolicy.ConvertName(name);
	}

	/// <summary>
	/// Determines whether the options use the built-in camel-case naming policy.
	/// </summary>
	/// <remarks>
	/// The method is obsolete, and a call to it doesn't compile.
	/// </remarks>
	/// <param name="options">The options to check, or <see langword="null"/>.</param>
	/// <returns>
	/// <see langword="true"/> if <see cref="JsonSerializerOptions.PropertyNamingPolicy"/> is
	/// <see cref="JsonNamingPolicy.CamelCase"/>; otherwise, <see langword="false"/>.
	/// </returns>
	[Obsolete("Complete rework", error: true)]
	public static bool HasCamelCaseNamingPolicy([NotNullWhen(true)] this JsonSerializerOptions? options)
	{
		return ReferenceEquals(JsonNamingPolicy.CamelCase, options?.PropertyNamingPolicy);
	}
}
