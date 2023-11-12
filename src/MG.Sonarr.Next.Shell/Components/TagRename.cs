using System.Text.Json.Serialization;

namespace MG.Sonarr.Next.Shell.Components
{
    public readonly struct TagRename
    {
        readonly string? _name;
        readonly int _id;
        readonly bool _isNotEmpty;

        public int Id => _id;

        [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
        [MemberNotNullWhen(false, nameof(_name))]
        public bool IsEmpty => !_isNotEmpty;

        public string Label => _name ?? string.Empty;

        private TagRename(in int id, string newLabel)
        {
            _isNotEmpty = true;
            _name = newLabel;
            _id = id;
        }

        public static TagRename Create(int id, string newLabel)
        {
            ArgumentException.ThrowIfNullOrEmpty(newLabel);

            return new(id, newLabel);
        }
    }
}

