using System;

namespace MG.Sonarr.Functionality
{
    /// <summary>
    /// An interfaces that exposes methods for displaying file sizes (in bytes)
    /// in larger/smaller numerical representations.
    /// </summary>
    public interface ICanCalculate
    {
        /// <summary>
        /// Represents the byte size of a given object metadata in physical storage
        /// sizes.  This value can be converted easily using the 
        /// <see cref="ICanCalculate.ToSize(ByteUnit)"/> methods in this interface.
        /// </summary>
        long SizeOnDisk { get; }

        /// <summary>
        /// Changes the "size" of a value (represented in bytes) to the
        /// specified multiple of the unit.
        /// </summary>
        /// <param name="inUnit"></param>
        /// <returns></returns>
        decimal ToSize(ByteUnit inUnit);
    }
}
