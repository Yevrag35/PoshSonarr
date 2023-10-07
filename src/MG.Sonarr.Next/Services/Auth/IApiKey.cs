namespace MG.Sonarr.Next.Services.Auth
{
    /// <summary>
    /// An interface exposing methods for retrieving API key <see cref="string"/> objects from the implementing
    /// object.
    /// </summary>
    public interface IApiKey
    {
        /// <summary>
        /// Gets the <see cref="string"/> value of the API key.
        /// </summary>
        string GetValue();
    }
}
