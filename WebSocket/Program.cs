using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.UseWebSockets();

//coleção thread-safe do .NET que armazena todos os WebSockets conectados.
//Thread = caminhos de execução independentes dentro de um mesmo processo,
var sockets = new ConcurrentDictionary<Guid, WebSocket>();

//histórico de mensagens (até 50 mensagens)
var history = new ConcurrentQueue<string>();
 
app.Map("/ws", async context =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
        return;
    }

    using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
    var id = Guid.NewGuid();

    sockets.TryAdd(id, webSocket);

    Console.WriteLine(history);
    foreach (var msg in history)
    {
        await webSocket.SendAsync(
            Encoding.UTF8.GetBytes(msg),
            WebSocketMessageType.Text,
            true,
            CancellationToken.None);
    }

    var buffer = new byte[1024];

    try
    {
        while (webSocket.State == WebSocketState.Open)
        {
            var result = await webSocket.ReceiveAsync(buffer, CancellationToken.None);

            if (result.MessageType == WebSocketMessageType.Close)
                break;

            Console.WriteLine($"Client ({id}) conectado");

            var message = Encoding.UTF8.GetString(buffer, 0, result.Count);

            history.Enqueue($"{id.ToString()[..5]}: {message}");

            // opcional: limitar histórico
            while (history.Count > 50)
                history.TryDequeue(out _);

            var data = Encoding.UTF8.GetBytes($"{id.ToString()[..5]}: {message}");

            foreach (var ws in sockets.Values)
            {
                if (ws.State == WebSocketState.Open)
                {
                    Console.WriteLine($"send to client ");
                    await ws.SendAsync(
                        data,
                        WebSocketMessageType.Text,
                        true,
                        CancellationToken.None);
                }
            }

        }
    }
    finally
    {
        sockets.TryRemove(id, out _);

        if (webSocket.State != WebSocketState.Closed)
        {
            await webSocket.CloseAsync(
                WebSocketCloseStatus.NormalClosure,
                "Closed",
                CancellationToken.None);
        }
    }


});

app.Run("http://localhost:5000");