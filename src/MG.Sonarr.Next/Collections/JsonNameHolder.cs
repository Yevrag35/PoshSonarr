namespace MG.Sonarr.Next.Collections
{
    internal readonly struct JsonNameHolder
    {
        readonly IReadOnlyDictionary<string, string> _deserialization;
        readonly IReadOnlyDictionary<string, string> _serialization;

        public IReadOnlyDictionary<string, string> DeserializationNames => _deserialization ?? EmptyNameDictionary<string>.Default;
        public IReadOnlyDictionary<string, string> SerializationNames => _serialization ?? EmptyNameDictionary<string>.Default;

        internal JsonNameHolder(IReadOnlyDictionary<string, string>? serialization, IReadOnlyDictionary<string, string>? deserialization)
        {
            _deserialization = deserialization ?? EmptyNameDictionary<string>.Default;
            _serialization = serialization ?? EmptyNameDictionary<string>.Default;
        }
    }
}
