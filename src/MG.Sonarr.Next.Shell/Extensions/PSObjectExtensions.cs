namespace MG.Sonarr.Next.Shell.Extensions
{
    internal static class PSObjectExtensions
    {
        internal static void AddNameAlias(this PSObject? pso)
        {
            pso?.Properties.Add(new PSAliasProperty(Constants.NAME, Constants.TITLE));
        }
    }
}
