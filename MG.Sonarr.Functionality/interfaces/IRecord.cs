using System;

namespace MG.Sonarr.Functionality
{
    public interface IRecord : IComparable<IRecord>, IEquatable<IRecord>
    {
        long Id { get; }
    }
}
