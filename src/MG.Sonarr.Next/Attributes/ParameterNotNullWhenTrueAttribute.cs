namespace MG.Sonarr.Next.Attributes;

/// <summary>
/// Indicates the decorated parameter will be not <see langword="null"/> even the Type allows it when the
/// other specified parameter is <see langword="true"/>.
/// </summary>
/// <remarks>
///     This attribute has no effect on the complier or C# analyzers.
/// </remarks>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
public sealed class ParameterNotNullWhenTrueAttribute : AnalysisAttribute
{
	/// <summary>
	/// Gets the name of the Boolean parameter whose <see langword="true"/> value guarantees that the decorated
	/// parameter isn't <see langword="null"/>.
	/// </summary>
	/// <value>The name of the other parameter.</value>
	public string ParameterName { get; }

	/// <summary>
	/// Initializes a new <see cref="ParameterNotNullWhenTrueAttribute"/> instance that names the Boolean parameter
	/// the decorated parameter's nullability depends on.
	/// </summary>
	/// <param name="parameterName">The name of the other parameter, typically given with <see langword="nameof"/>.</param>
	public ParameterNotNullWhenTrueAttribute(string parameterName)
	{
		this.ParameterName = parameterName;
	}
}
