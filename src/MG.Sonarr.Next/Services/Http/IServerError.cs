namespace MG.Sonarr.Next.Services.Http;

public interface IServerError
{
	string? Description { get; }
	string? Message { get; }
	string? Title { get; }
	int? StatusCode { get; }
	string? TraceId { get; }
}
