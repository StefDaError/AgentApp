using System.Net.WebSockets;
using System.Text;

namespace AgentApp.Transport;

public sealed class WebSocketClientTransport : IWebSocketTransport
{
    private ClientWebSocket? _ws;

    public async Task ConnectAsync(Uri url, CancellationToken ct)
    {
        _ws = new ClientWebSocket();
        _ws.Options.KeepAliveInterval = TimeSpan.FromSeconds(20);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(5));

        try
        {
            await _ws.ConnectAsync(url, timeoutCts.Token);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !ct.IsCancellationRequested)
        {
            throw new TimeoutException($"Не удалось подключиться к {url} за 5 секунд");
        }
    }

    public async Task SendAsync(string message, CancellationToken ct)
    {
        if (_ws is null || _ws.State != WebSocketState.Open)
            throw new InvalidOperationException("Соединение не установлено");

        var bytes = Encoding.UTF8.GetBytes(message);
        await _ws.SendAsync(bytes, WebSocketMessageType.Text, endOfMessage: true, ct);
    }

    public async Task CloseAsync(CancellationToken ct)
    {
        if (_ws is { State: WebSocketState.Open })
        {
            try
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
                timeout.CancelAfter(TimeSpan.FromSeconds(2));
                await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", timeout.Token);
            }
            catch { /* сервер мог уже отвалиться — не падаем */ }
        }

        _ws?.Dispose();
        _ws = null;
    }

    public async ValueTask DisposeAsync() => await CloseAsync(CancellationToken.None);
}