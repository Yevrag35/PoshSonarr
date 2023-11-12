namespace MG.Sonarr.Next.Components
{
    public ref struct DoubleBool
    {
        public bool Bool1;
        public bool Bool2;

        private DoubleBool(bool initialize)
        {
            Bool1 = initialize;
            Bool2 = initialize;
        }

        public static DoubleBool InitializeNew() => new(false);

        public static implicit operator bool(DoubleBool dub) => dub.Bool1 && dub.Bool2;
    }
}

