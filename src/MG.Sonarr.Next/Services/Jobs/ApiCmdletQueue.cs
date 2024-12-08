using MG.Sonarr.Next.Services.Http;
using System.Collections.Concurrent;

namespace MG.Sonarr.Next.Services.Jobs;

/// <summary>
/// A queue that holds <see cref="IApiCmdlet"/> instances to be processed by services.
/// </summary>
public sealed class ApiCmdletQueue
{
    private readonly ConcurrentQueue<IApiCmdlet> _queue;

    /// <summary>
    /// Gets the number of cmdlets contained in the <see cref="ApiCmdletQueue"/>.
    /// </summary>
    internal int Count => _queue.Count;
    /// <summary>
    /// Gets a value indicating whether the <see cref="ApiCmdletQueue"/> is empty.
    /// </summary>
    public bool IsEmpty => _queue.IsEmpty;

    public ApiCmdletQueue()
    {
        _queue = [];
    }

    public void Enqueue(IApiCmdlet cmdlet)
    {
        _queue.Enqueue(cmdlet);
    }

    public bool TryDequeue([NotNullWhen(true)] out IApiCmdlet? cmdlet)
    {
        return _queue.TryDequeue(out cmdlet);
    }
}
