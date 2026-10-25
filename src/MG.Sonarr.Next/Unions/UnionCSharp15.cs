#if NET11_0_OR_GREATER
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Runtime.CompilerServices.IUnion))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Runtime.CompilerServices.UnionAttribute))]
#else
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace System.Runtime.CompilerServices;
#pragma warning restore IDE0130
/// <summary>
/// Forwards to <see cref="IUnion"/> for non-NET11 projects.
/// </summary>
public interface IUnion
{
	/// <summary>
	/// Gets the value contained in the union, or <see langword="null" /> if the union has no value.
	/// </summary>
	/// <value>
	/// The current value of the union as one of its case types, or <see langword="null" />.
	/// </value>
	object? Value { get; }
}

/// <summary>
/// Indicates that a class or struct is a union type, enabling compiler support for union behaviors.
/// </summary>
/// <remarks>
/// This is the non-forwarded version for .NET 10 and earlier and has no effect when compiled said frameworks.
/// <para>
/// Any class or struct annotated with this attribute is recognized by the C# compiler as a union type.
/// Union types may support behaviors such as implicit conversions from case types, pattern matching
/// that unwraps the union's contents, and switch exhaustiveness checking.
/// </para>
/// </remarks>
/// <seealso cref="IUnion" />
[AttributeUsage(AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
internal sealed class UnionAttribute : Attribute
{
}
#endif
