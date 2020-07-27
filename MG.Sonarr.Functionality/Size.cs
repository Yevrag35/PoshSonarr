using MG.Api.Json;
using Newtonsoft.Json;
using System;

namespace MG.Sonarr.Functionality
{
    public struct Size : IComparable, IComparable<decimal>, IComparable<int>, IComparable<long>, IComparable<Size>, IEquatable<int>, IEquatable<long>, IJsonObject
    {
        internal const decimal Kilobytes = 1024M;
        internal const decimal Megabytes = 1048576M;           //1,048,576
        internal const decimal Gigabytes = 1073741824M;        //‭1,073,741,824‬
        internal const decimal Terabytes = 1099511627776M;     //‭1,099,511,627,776‬

        private const int DEFAULT_PLACES = 2;
        private const MidpointRounding ROUND = MidpointRounding.AwayFromZero;

        private bool _biggerThanInt;
        /// <summary>
        /// The number of bytes the size is.
        /// </summary>
        public long Bytes;
        public decimal KB;
        public decimal MB;
        public decimal GB;
        public decimal TB;

        public Size(long bytes)
        {
            _biggerThanInt = bytes > int.MaxValue;
            this.Bytes = bytes;
            this.KB = ToSize(bytes, ByteUnit.KB);
            this.MB = ToSize(bytes, ByteUnit.MB);
            this.GB = ToSize(bytes, ByteUnit.GB);
            this.TB = ToSize(bytes, ByteUnit.TB);
        }

        public int CompareTo(decimal dec) => this.Bytes.CompareTo(dec);
        public int CompareTo(int num) => this.Bytes.CompareTo(num);
        public int CompareTo(long num) => this.Bytes.CompareTo(num);
        public int CompareTo(Size other) => this.Bytes.CompareTo(other.Bytes);
        public int CompareTo(object obj)
        {
            if (obj is Size other)
                return this.CompareTo(other);

            else
                return -1;
        }
        public override bool Equals(object obj)
        {
            if (obj is Size other)
                return this.Bytes.Equals(other.Bytes);

            else
                return this.Bytes.Equals(obj);
        }
        public bool Equals(int value) => this.Bytes.Equals(value);
        public bool Equals(long value) => this.Bytes.Equals(value);
        internal static decimal Calculate(long sizeInBytes, decimal divideBy, int numberOfDecimalPlaces)
        {
            if (numberOfDecimalPlaces > -1)
            {
                return Math.Round(
                    sizeInBytes / divideBy,
                    numberOfDecimalPlaces,
                    ROUND
                );
            }
            else
            {
                return sizeInBytes / divideBy;
            }
        }
        public override int GetHashCode() => this.Bytes.GetHashCode();
        public decimal ToSize(ByteUnit inUnit, int decimalPlaces) => ToSize(this.Bytes, inUnit, decimalPlaces);
        private static decimal ToSize(long bytes, ByteUnit inUnit, int decimalPlaces = DEFAULT_PLACES)
        {
            switch (inUnit)
            {
                case ByteUnit.MB:
                    return Calculate(bytes, Megabytes, decimalPlaces);

                case ByteUnit.KB:
                    return Calculate(bytes, Kilobytes, decimalPlaces);

                case ByteUnit.TB:
                    return Calculate(bytes, Terabytes, decimalPlaces);

                default:
                    return Calculate(bytes, Gigabytes, decimalPlaces);
            }
        }

        public string ToJson() => this.Bytes.ToString();
        string IJsonObject.ToJson(System.Collections.IDictionary parameters) => this.ToJson();

        public static explicit operator Size(int number) => new Size(Convert.ToInt64(number));
        public static implicit operator Size(long number) => new Size(number);
        public static implicit operator Size(short number) => new Size(Convert.ToInt64(number));

        public static implicit operator Size(uint number) => new Size(Convert.ToInt64(number));
        public static implicit operator Size(ulong number) => new Size(Convert.ToInt64(number));
        public static implicit operator Size(ushort number) => new Size(Convert.ToInt64(number));

        public static implicit operator Size(byte b) => new Size(Convert.ToInt64(b));

        public static implicit operator long(Size size) => size.Bytes;

        //public override string ToString() => this.Bytes.ToString();
    }
}
