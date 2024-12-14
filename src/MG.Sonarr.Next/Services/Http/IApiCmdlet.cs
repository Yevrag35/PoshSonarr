using MG.Sonarr.Next.Services.Http.Clients;
using MG.Sonarr.Next.Services.Http.Handlers;

namespace MG.Sonarr.Next.Services.Http
{
    /// <summary>
    /// An interface that allows for the <see cref="VerboseHandler"/> to write custom messages before and after
    /// any API request is sent with a <see cref="ISonarrClient"/> instance.
    /// </summary>
    public interface IApiCmdlet
    {
        /// <summary>
        /// Indicates whether the cmdlet should serialize the JSON payloads to the PowerShell's debug stream before sending HTTP requests.
        /// </summary>
        bool CanDebugSerializeBefore { get; }
        /// <summary>
        /// Indicates whether the cmdlet should serialize the JSON payloads to the PowerShell's debug stream after receiving HTTP responses.
        /// </summary>
        bool CanDebugSerializeAfter { get; }

        /// <summary>
        /// Writes a JSON payload to the PowerShell's debug stream. The payload can either be the request or response serialized.
        /// </summary>
        /// <param name="jsonPayload">The JSON serialized payload to write to the debug stream.</param>
        void WriteDebugPayload(string jsonPayload);

        /// <summary>
        /// Writes a message to the PowerShell's verbose stream before the provided request message is sent.
        /// </summary>
        /// <param name="request">The details of the pending request.</param>
        void WriteVerboseBefore(IHttpRequestDetails request);
        /// <summary>
        /// Writes a message to the PowerShell's verbose stream after the a response is received from 
        /// an API request.
        /// </summary>
        /// <param name="response">The returning response details of the message.</param>
        /// <param name="provider">A scoped service provider to construct/retrieve any services.</param>
        /// <param name="options">The JSON serialization options being used by the deserialization
        /// process.</param>
        void WriteVerboseAfter(ISonarrResponse response, IServiceProvider provider, JsonSerializerOptions? options);
    }
}
