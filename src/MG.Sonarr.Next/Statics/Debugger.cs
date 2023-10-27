namespace MG.Sonarr.Next
{
    public static class Debugger
    {
        [Conditional("DEBUG")]
        [DebuggerStepThrough]
        public static void Assert(Func<bool> predicate)
        {
            Debug.Assert(predicate());
        }

        [Conditional("DEBUG")]
        [DebuggerStepThrough]
        public static void Assert(Func<bool> predicate, string? message)
        {
            Debug.Assert(predicate(), message);
        }

        [Conditional("DEBUG")]
        [DebuggerStepThrough]
        public static void Assert(Func<bool> predicate, string? message, string? detailedMessage)
        {
            Debug.Assert(predicate(), message, detailedMessage);
        }
    }
}

