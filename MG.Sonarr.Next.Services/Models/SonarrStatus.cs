using MG.Sonarr.Next.Services.Auth;

namespace MG.Sonarr.Next.Services.Models
{
    public sealed class SonarrStatus
    {
        public string? Authentication { get; set; }

        public bool TryGetAuthType(out SonarrAuthType authType)
        {
            return Enum.TryParse(this.Authentication, ignoreCase: true, out authType);
        }
    }
}
