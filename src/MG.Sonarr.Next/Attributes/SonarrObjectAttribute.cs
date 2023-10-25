using MG.Sonarr.Next.Models;
using System.Text.Json.Serialization;

namespace MG.Sonarr.Next.Attributes
{
    /// <summary>
    /// An internal <see cref="Attribute"/> decorated on <see cref="SonarrObject"/> derived classes.
    /// </summary>
    /// <remarks>
    ///     When a connection is established with a Sonarr server, a reflection method will resolve all classes 
    ///     decorated with this attribute and create a corresponding <see cref="JsonConverter"/> for 
    ///     JSON serialization/deserialization as part of the Dependency Injection service collection creation.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    internal sealed class SonarrObjectAttribute : Attribute
    {
    }
}
