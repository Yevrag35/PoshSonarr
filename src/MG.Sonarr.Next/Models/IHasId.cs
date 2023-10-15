namespace MG.Sonarr.Next.Models
{
    /// <summary>
    /// An interface exposing the Id property of an implementing object.
    /// </summary>
    public interface IHasId
    {
        /// <summary>
        /// The Sonarr database ID of the implementing object.
        /// </summary>
        int Id { get; }
    }
}
