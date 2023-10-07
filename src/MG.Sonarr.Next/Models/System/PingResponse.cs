using System.Text.Json.Serialization;

namespace MG.Sonarr.Next.Models.System
{
    public sealed class PingResponse
    {
        const string PONG = "pong";

        public string Response { get; init; } = string.Empty;

        [MemberNotNullWhen(true, nameof(Response))]
        [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
        public bool IsPong => PONG.Equals(this.Response, StringComparison.InvariantCultureIgnoreCase);
    }
}
