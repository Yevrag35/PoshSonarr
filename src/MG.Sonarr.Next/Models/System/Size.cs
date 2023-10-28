using System.ComponentModel;
using System.Management.Automation;

namespace MG.Sonarr.Next.Models.System
{
    public readonly struct Size : IComparable, IComparable<Size>, IEquatable<Size>,
        IComparable<int>,
        IComparable<long>,
        IEquatable<int>,
        IEquatable<long>
    {
        const int DEFAULT_PRECISION = 2;
        const double ONE_MB = 1_048_576d;
        const double ONE_GB = 1_073_741_824d;
        const double ONE_TB = 1_099_511_627_776d;

        readonly long _bytes;
        readonly double _inMb;
        readonly double _inGb;
        readonly double _inTb;

        public double InMB => _inMb;
        public double InGB => _inGb;
        public double InTB => _inTb;
        public long TotalBytes => _bytes;

        public Size(in long bytes)
            : this(in bytes, DEFAULT_PRECISION)
        {
        }
        public Size(in long bytes, in int roundingPrecision)
        {
            _bytes = bytes;
            _inMb = CalculateSize(in bytes, ONE_MB, in roundingPrecision);
            _inGb = CalculateSize(in bytes, ONE_GB, in roundingPrecision);
            _inTb = CalculateSize(in bytes, ONE_TB, in roundingPrecision);
        }

        private static double CalculateSize(in long bytes, double divideBy, in int precision)
        {
            double divide = bytes / divideBy;
            return Math.Round(divide, precision, MidpointRounding.AwayFromZero);
        }

        public int CompareTo(object? other)
        {
            return other is int intVal
                ? _bytes.CompareTo(intVal)
                : other is Size size
                    ? _bytes.CompareTo(size._bytes)
                    : _bytes.CompareTo(other);
        }
        public int CompareTo(Size other)
        {
            return this.CompareTo(other._bytes);
        }
        public int CompareTo(long other)
        {
            return _bytes.CompareTo(other);
        }
        public int CompareTo(int other)
        {
            return this.CompareTo((long)other);
        }
        public bool Equals(Size other)
        {
            return this.Equals(other._bytes);
        }
        public bool Equals(int other)
        {
            return _bytes == other;
        }
        public bool Equals(long other)
        {
            return _bytes == other;
        }
        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj is Size size)
            {
                return this.Equals(size);
            }
            else if (obj is long || obj is int)
            {
                return this.Equals((long)obj);
            }

            return false;
        }
        public override int GetHashCode()
        {
            return _bytes.GetHashCode();
        }

        public static implicit operator Size(int bytes)
        {
            return new(bytes);
        }
        public static explicit operator Size(long bytes)
        {
            return new(in bytes);
        }

        public static bool operator ==(Size x, Size y)
        {
            return x.Equals(y);
        }
        public static bool operator !=(Size x, Size y)
        {
            return !(x == y);
        }
        public static bool operator ==(Size x, long y)
        {
            return x.Equals(y);
        }
        public static bool operator !=(Size x, long y)
        {
            return !(x == y);
        }
        public static bool operator ==(long x, Size y)
        {
            return y.Equals(x);
        }
        public static bool operator !=(long x, Size y)
        {
            return !(x == y);
        }
        public static bool operator ==(Size x, int y)
        {
            return x.Equals(y);
        }
        public static bool operator !=(Size x, int y)
        {
            return !(x == y);
        }
        public static bool operator ==(int x, Size y)
        {
            return y.Equals(x);
        }
        public static bool operator !=(int x, Size y)
        {
            return !(x == y);
        }

        public static bool operator <(Size left, Size right)
        {
            return left.CompareTo(right) < 0;
        }
        public static bool operator <=(Size left, Size right)
        {
            return left.CompareTo(right) <= 0;
        }
        public static bool operator >(Size left, Size right)
        {
            return left.CompareTo(right) > 0;
        }
        public static bool operator >=(Size left, Size right)
        {
            return left.CompareTo(right) >= 0;
        }

        public static bool operator <(Size left, int right)
        {
            return left.CompareTo(right) < 0;
        }
        public static bool operator <=(Size left, int right)
        {
            return left.CompareTo(right) <= 0;
        }
        public static bool operator >(Size left, int right)
        {
            return left.CompareTo(right) > 0;
        }
        public static bool operator >=(Size left, int right)
        {
            return left.CompareTo(right) >= 0;
        }
        public static bool operator <(int left, Size right)
        {
            return ((long)left).CompareTo(right._bytes) < 0;
        }
        public static bool operator <=(int left, Size right)
        {
            return ((long)left).CompareTo(right._bytes) <= 0;
        }
        public static bool operator >(int left, Size right)
        {
            return ((long)left).CompareTo(right._bytes) > 0;
        }
        public static bool operator >=(int left, Size right)
        {
            return ((long)left).CompareTo(right._bytes) >= 0;
        }

        public static bool operator <(Size left, long right)
        {
            return left.CompareTo(right) < 0;
        }
        public static bool operator <=(Size left, long right)
        {
            return left.CompareTo(right) <= 0;
        }
        public static bool operator >(Size left, long right)
        {
            return left.CompareTo(right) > 0;
        }
        public static bool operator >=(Size left, long right)
        {
            return left.CompareTo(right) >= 0;
        }
        public static bool operator <(long left, Size right)
        {
            return left.CompareTo(right._bytes) < 0;
        }
        public static bool operator <=(long left, Size right)
        {
            return left.CompareTo(right._bytes) <= 0;
        }
        public static bool operator >(long left, Size right)
        {
            return left.CompareTo(right._bytes) > 0;
        }
        public static bool operator >=(long left, Size right)
        {
            return left.CompareTo(right._bytes) >= 0;
        }
    }
}

