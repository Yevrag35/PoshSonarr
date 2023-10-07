namespace MG.Sonarr.Next.Services.Auth
{
    public enum SonarrAuthType
    {
        None,
        Basic,
        Forms,
        /// <summary>
        /// This will be a thing in Sonarr v4.
        /// </summary>
        External,
    }
}
