#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace System.Runtime.CompilerServices;

/// <summary>
/// Represents the modifier that the compiler requires on every <see langword="init"/> accessor, including the ones that
/// records generate for their positional properties.
/// </summary>
/// <remarks>
/// netstandard2.0 doesn't declare this type, and the compiler accepts any type of this name, so this internal copy stands in.
/// </remarks>
internal static class IsExternalInit
{
}

#pragma warning restore IDE0130
