namespace AgentApp.Transport;

public interface IWebSocketTransport : IAsyncDisposable
{
    Task ConnectAsync(Uri url, CancellationToken ct);
    Task SendAsync(string message, CancellationToken ct);
    Task CloseAsync(CancellationToken ct);
}