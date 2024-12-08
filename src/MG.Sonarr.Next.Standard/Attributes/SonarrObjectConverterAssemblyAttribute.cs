using System;

namespace MG.Sonarr.Next.Attributes
{
    /// <summary>
    /// An attribute decorated on assemblies that contain SonarrObject derived classes meaning that they should be scanned
    /// for JSON serialization/deserialization converters on module load.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
    public sealed class SonarrObjectConverterAssemblyAttribute : Attribute
    {
    }
}
