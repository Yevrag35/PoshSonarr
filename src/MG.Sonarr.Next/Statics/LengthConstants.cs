using System.Numerics;
using System.Runtime.InteropServices;

namespace MG.Sonarr.Next
{
    public static class LengthConstants
    {
        public const int BYTE_MAX = 3;
        public const int INT_MAX = 11;
        public const int LONG_MAX = 20;
        public const int FLOAT_MAX = 14;
        public const int UINT_MAX = INT_MAX - 1;
        public const int ULONG_MAX = LONG_MAX;
        public const int DOUBLE_MAX = 24;    // double.MinValue.ToString().Length
        public const int DECIMAL_MAX = 30;
        public const int SHORT_MAX = 6;
        public const int USHORT_MAX = SHORT_MAX - 1;
        public const int CHAR_MAX = SHORT_MAX - 1;
        public const int HALF_MAX = SHORT_MAX;
        public const int INT128_MAX = 40;
        public const int UINT128_MAX = INT128_MAX - 1;
        public const int INT_PTR_MAX = LONG_MAX;
        public const int NFLOAT_MAX = 24;
        public const int UINT_PTR_MAX = INT_PTR_MAX;
        public const int SBYTE_MAX = BYTE_MAX + 1;

        public const int GUID_FORM_B_OR_P = 38;
        public const int GUID_FORM_N = 32;
        public const int GUID_FORM_D = 36;
        public const int GUID_FORM_X = 68;

        public const int HTTP_STATUS_CODE_MAX = 29; // Enum.GetNames<HttpStatusCode>().Max(x => x.Length)

        public static int GetMaxLength<T>(ref readonly T number) where T : unmanaged, INumber<T>
        {
            return number switch
            {
                int => INT_MAX,
                long => LONG_MAX,
                uint => UINT_MAX,
                ulong => ULONG_MAX,
                float => NFLOAT_MAX,
                double => DOUBLE_MAX,
                decimal => DECIMAL_MAX,
                short => SHORT_MAX,
                ushort => USHORT_MAX,
                byte => BYTE_MAX,
                sbyte => SBYTE_MAX,
                char => CHAR_MAX,
                IntPtr => INT_PTR_MAX,
                UIntPtr => UINT_PTR_MAX,
                Half => HALF_MAX,
                UInt128 => UINT128_MAX,
                Int128 => INT128_MAX,
                NFloat => NFLOAT_MAX,
                BigInteger => INT128_MAX,
                _ => INT128_MAX,
            };
        }
    }
}