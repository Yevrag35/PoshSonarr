namespace MG.Sonarr.Next.Shell.Extensions
{
    public static class PSIntrinsicsExtensions
    {
        public static bool TryGetVariableValue<T>(this PSVariableIntrinsics? intrinsics, string variableName, [NotNullWhen(true)] out T? value)
        {
            if (intrinsics is not null)
            {
                object? val = intrinsics.GetValue(variableName);
                if (val is T tVal)
                {
                    value = tVal;
                    return true;
                }
            }

            value = default;
            return false;
        }
    }
}
