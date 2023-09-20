namespace MG.Sonarr.Next.Services
{
    public static class LengthConstants
    {
        public const int INT_MAX = 11;
        public const int LONG_MAX = 20;
        public const int UINT_MAX = INT_MAX - 1;
        public const int ULONG_MAX = LONG_MAX;
        public const int DOUBLE_MAX = 24;    // double.MinValue.ToString().Length
        public const int DECIMAL_MAX = 30;
        public const int INT128_MAX = 40;

        public const int GUID_FORM_B_OR_P = 38;
        public const int GUID_FORM_N = 32;
        public const int GUID_FORM_D = 36;
        public const int GUID_FORM_X = 68;
    }
}