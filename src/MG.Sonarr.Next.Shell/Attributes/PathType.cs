namespace MG.Sonarr.Next.Shell.Attributes
{
    [Flags]
    public enum PathType
    {
        None = 0,
        Possible = 1,

        Local = 2,
        UNC = 4,

        Absolute = 16,
        Relative = 32,
    }
}

