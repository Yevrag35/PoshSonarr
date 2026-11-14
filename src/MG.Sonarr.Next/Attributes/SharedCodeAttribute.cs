namespace MG.Sonarr.Next.Attributes;

/// <summary>
/// Indicates that a type comes from a shared repository of code that several solutions copy and adapt.
/// </summary>
/// <remarks>
/// <para>
/// A marked type stays even when nothing in this solution calls it, since another solution, or a later change to this
/// one, may need it. It should preferably also carry <see cref="ExcludeFromCodeCoverageAttribute"/> where the target
/// framework allows that attribute, so that the members that this solution never calls don't lower this solution's
/// coverage. No framework allows it on an enum or a delegate, and none before .NET 11 allows it on an interface. A type
/// nested in a marked type needs neither attribute of its own.
/// </para>
/// <para>
/// The attribute exists only to be seen in source. It's conditional on <c>SHARED_CODE_METADATA</c>, a symbol that no
/// build defines, so the compiler leaves it out of the assembly's metadata.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface | AttributeTargets.Enum | AttributeTargets.Delegate, AllowMultiple = false, Inherited = false)]
[Conditional("SHARED_CODE_METADATA"), ExcludeFromCodeCoverage]
internal sealed class SharedCodeAttribute : Attribute;
