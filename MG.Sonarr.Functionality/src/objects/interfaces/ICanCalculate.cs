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
        /// Represents the byte size of a given object in physical storage.  
        /// This value can be converted easily using the 
        /// <see cref="ICanCalculate.ToSize(ByteUnit)"/> methods in this interface.
        /// </summary>
        long SizeOnDisk { get; }

        /// <summary>
        /// Changes the <see cref="ICanCalculate.SizeOnDisk"/> value to the
        /// specified multiple of the unit of bytes.
        /// </summary>
        /// <param name="inUnit">The multiple of a "byte" unit to divide the size by.</param>
        decimal ToSize(ByteUnit inUnit);

        /// <summary>
        /// Changes the <see cref="ICanCalculate.SizeOnDisk"/> value to the
        /// specified multiple of the unit of bytes with the result rounded to the 
        /// specified number of decimal places.
        /// </summary>
        /// <param name="inUnit">The multiple of a "byte" unit to divide the size by.</param>
        /// <param name="decimalPlaces">The number of decimal places to round the result to.</param>
        decimal ToSize(ByteUnit inUnit, int decimalPlaces);
    }
}
