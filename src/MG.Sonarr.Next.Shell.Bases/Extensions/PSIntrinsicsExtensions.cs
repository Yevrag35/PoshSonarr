namespace MG.Sonarr.Next.Shell.Extensions
{
    public static class PSIntrinsicsExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="intrinsics"></param>
        /// <param name="variableName"></param>
        /// <param name="value"></param>
        /// <exception cref="ArgumentException"/>
        /// <exception cref="ArgumentNullException"/>
        /// <returns></returns>
        public static bool TryGetVariableValue<T>(this PSVariableIntrinsics intrinsics, string variableName, [NotNullWhen(true)] out T? value)
        {
            ArgumentNullException.ThrowIfNull(intrinsics);
            ArgumentException.ThrowIfNullOrEmpty(variableName);

            object? val;
            try
            {
                val = intrinsics.GetValue(variableName);
            }
            catch (Exception e)
            {
                Debug.Fail(e.Message);
                value = default;
                return false;
            }

            if (val is T tVal)
            {
                value = tVal;
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }
    }
}
