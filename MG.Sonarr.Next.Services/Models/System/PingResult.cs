using MG.Sonarr.Next.Services.Extensions;
using MG.Sonarr.Next.Services.Http;
using System.Management.Automation;
using System.Net;

namespace MG.Sonarr.Next.Services.Models.System
{
    public sealed class PingResult
    {
        public bool IsSuccess { get; }
        public TimeSpan Duration { get; }
        public string Message { get; }
        public string Status { get; }
        public ErrorRecord? Error { get; }

        public PingResult(in SonarrResponse<PingResponse> response, long elapsedTicks)
        {
            this.IsSuccess = !response.IsEmpty && !response.IsError;
            this.Duration = TimeSpan.FromTicks(elapsedTicks);

            this.Message = response.Data?.Response ?? string.Empty;
            this.Status = GetHttpStatusString(response.StatusCode);
            this.Error = response.Error;
        }

        private static string GetHttpStatusString(HttpStatusCode code)
        {
            string codeStr = code.ToString();
            int asInt = (int)code;

            if (asInt > 999 || asInt < 0)
            {
                return codeStr;
            }

            Span<char> span = stackalloc char[codeStr.Length + 6];
            _ = asInt.TryFormat(span, out int written, default, Statics.DefaultProvider);
            int position = written;

            (stackalloc char[] { ' ', '(' }).CopyToSlice(span, ref position);
            codeStr.CopyToSlice(span, ref position);

            span[position++] = ')';
            return new string(span.Slice(0, position));
        }
    }
}
