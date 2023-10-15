using System.Numerics;

namespace MG.Sonarr.Next.Metadata
{
    public struct NumericHolder<T> : IComparable<T>, IEquatable<T>, IComparable<NumericHolder<T>>, IEquatable<NumericHolder<T>>
        where T : unmanaged, IComparable<T>, IEquatable<T>, INumber<T>
    {
        private T _value;

        public T Value
        {
            readonly get => _value;
            set => _value = value;
        }

        public NumericHolder(in T value)
        {
            _value = value;
        }

        public static NumericHolder<T> Default => new(default);

        public readonly int CompareTo(T other)
        {
            return other.CompareTo(_value);
        }
        public readonly int CompareTo(NumericHolder<T> other)
        {
            return this.CompareTo(other._value);
        }
        public readonly bool Equals(NumericHolder<T> other)
        {
            return this.Equals(other._value);
        }
        public readonly bool Equals(T other)
        {
            return other.Equals(_value);
        }
        public readonly override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj is NumericHolder<T> holder)
            {
                return this.Equals(holder.Value);
            }
            else if (obj is T other)
            {
                return this.Equals(other);
            }

            return false;
        }
        public readonly override int GetHashCode()
        {
            return _value.GetHashCode();
        }

        public static implicit operator NumericHolder<T>(int value)
        {
            return new(T.CreateChecked(value));
        }
        public static explicit operator T(NumericHolder<T> holder)
        {
            return holder._value;
        }

        public static bool operator ==(NumericHolder<T> x, NumericHolder<T> y)
        {
            return x.Equals(y);
        }
        public static bool operator !=(NumericHolder<T> x, NumericHolder<T> y)
        {
            return !(x == y);
        }
        public static bool operator ==(NumericHolder<T> x, T y)
        {
            return x.Equals(y);
        }
        public static bool operator !=(NumericHolder<T> x, T y)
        {
            return !(x == y);
        }
        public static bool operator ==(T x, NumericHolder<T> y)
        {
            return y.Equals(x);
        }
        public static bool operator !=(T x, NumericHolder<T> y)
        {
            return !(x == y);
        }
    }
}
