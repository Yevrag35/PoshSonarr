namespace MG.Sonarr.Next.Buffers;

/// <summary>
/// Represents a strongly-typed managed function pointer with three parameters and a return value.
/// </summary>
/// <remarks>Use this struct to encapsulate and invoke managed function pointers with three parameters in a
/// type-safe manner. This is intended for advanced scenarios where direct function pointer invocation is required, performance-critical code.
/// The struct is immutable and does not own the lifetime of the underlying
/// function pointer.</remarks>
/// <typeparam name="T0">The type of the first parameter passed to the function. Must be a type that allows ref struct constraints.</typeparam>
/// <typeparam name="T1">The type of the second parameter passed to the function. Must be a type that allows ref struct constraints.</typeparam>
/// <typeparam name="T2">The type of the third parameter passed to the function. Must be a type that allows ref struct constraints.</typeparam>
/// <typeparam name="TResult">The type of the value returned by the function.</typeparam>
[StructLayout(LayoutKind.Sequential)]
public readonly unsafe struct FnPtr<T0, T1, T2, TResult>
    where T0 : allows ref struct
    where T1 : allows ref struct
    where T2 : allows ref struct
{
    private readonly delegate* managed<T0, T1, T2, TResult> _ptr;

    public FnPtr(delegate* managed<T0, T1, T2, TResult> ptr)
    {
        _ptr = ptr;
    }

    public TResult Invoke(T0 arg0, T1 arg1, T2 arg2)
    {
        return _ptr(arg0, arg1, arg2);
    }
}