using MG.Api.Json;
using System;

namespace MG.Sonarr.Functionality
{
    public interface IQuality : IComparable<IQuality>, IEquatable<IQuality>, IJsonObject
    {
        int Id { get; }
        string Name { get; }
        int Resolution { get; }
        QualitySource Source { get; }
    }
}
