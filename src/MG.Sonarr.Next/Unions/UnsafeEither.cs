namespace MG.Sonarr.Next.Unions
{
    internal readonly struct UnsafeEither<T1, T2> where T1 : class where T2 : class
    {
        private readonly object? _value;
        internal object? Value => _value;

        internal UnsafeEither(T1 value)
        {
            _value = value;
        }
        internal UnsafeEither(T2 value)
        {
            _value = value;
        }


    }
}
