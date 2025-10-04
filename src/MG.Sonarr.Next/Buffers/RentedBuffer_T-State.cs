namespace MG.Sonarr.Next.Buffers;

file static class RentedBufferConstants
{
    internal const byte None = 0x0;
    internal const byte RentedBit = 0x1;
    internal const byte ClearBit = 0x2;
}

public ref partial struct RentedBuffer<T>
{
    /// <summary>
    /// Represents the state of a rented buffer, including flags for its rental status and whether it should be cleared on
    /// disposal.
    /// </summary>
    /// <remarks>This structure is used internally to manage the state of rented buffers. It provides properties to
    /// track whether the buffer is currently rented and whether it should be cleared when disposed. The state is stored as
    /// a compact bit field for efficiency.</remarks>
    [DebuggerDisplay(@"\{ State = {_state} \}")]
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 2)]
    private ref struct State
    {
        private ushort _state;

        internal bool ClearOnDispose
        {
            readonly get => (_state & RentedBufferConstants.ClearBit) != 0;
            set => _state = (ushort)((_state & ~RentedBufferConstants.ClearBit) | Mask(value, RentedBufferConstants.ClearBit));
        }
        internal bool IsRented
        {
            readonly get => (_state & RentedBufferConstants.RentedBit) != 0;
            set => _state = (ushort)((_state & ~RentedBufferConstants.RentedBit) | Mask(value, RentedBufferConstants.RentedBit));
        }

        public State()
        {
            _state = RentedBufferConstants.None;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ushort Mask(bool value, [ConstantExpected] byte bit)
        {
            return (ushort)((-Unsafe.As<bool, byte>(ref value)) & bit);
        }
    }
}

