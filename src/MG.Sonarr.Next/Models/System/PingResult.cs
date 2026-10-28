using MG.Sonarr.Next.Buffers;
using MG.Sonarr.Next.Services.Http;
using System.Management.Automation;
using System.Net;

namespace MG.Sonarr.Next.Models.System;

public sealed class PingResult
{
	public bool IsSuccess { get; }
	public TimeSpan Duration { get; }
	public string Message { get; }
	public string Status { get; }
	public ErrorRecord? Error { get; }

	public PingResult(SonarrClientResult<PingResponse> response, long elapsedTicks)
	{
		this.IsSuccess = !response.IsError;
		this.Duration = TimeSpan.FromTicks(elapsedTicks);

		this.Message = response.Value?.Response ?? string.Empty;
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

		using SpanStringBuilder builder = new(codeStr.Length + 6);
		builder.Append(asInt, LengthConstants.INT_MAX, format: default, Statics.DefaultProvider);
		builder.AppendChars(' ', '(');
		builder.Append(codeStr);
		builder.Append(')');

		return builder.ToString();
	}
}
