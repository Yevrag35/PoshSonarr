namespace MG.Sonarr.Next.Attributes;

/// <summary>
/// Marks a model class for a JSON converter of its own.
/// </summary>
/// <remarks>
/// <para>
/// A source generator lists a <see cref="MG.Sonarr.Next.Json.Converters.SonarrObjectConverter{T}"/> for each marked
/// class, and every connection's JSON options create them, so they read and write the model through its note properties
/// rather than its C# properties. Finding the models and creating their converters takes no reflection.
/// </para>
/// <para>
/// The attribute isn't inherited, so a model derived from another model needs its own. A marked class that the converter
/// can't take fails the build with a diagnostic on the attribute: one that is static, abstract, generic, or without a
/// public parameterless constructor, one that doesn't derive from <see cref="MG.Sonarr.Next.Models.SonarrObject"/> or
/// implement <see cref="MG.Sonarr.Next.Json.ISerializableNames{T}"/> of itself, and one that the generated list can't
/// name, such as a private nested class.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false), ExcludeFromCodeCoverage]
internal sealed class SonarrObjectAttribute : Attribute
{
}
