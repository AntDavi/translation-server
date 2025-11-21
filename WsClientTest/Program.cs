using System;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        Console.Write("ClientId (ex: player_1): ");
        string clientId = Console.ReadLine()!.Trim();

        Console.Write("RoomId (ex: room_abc): ");
        string roomId = Console.ReadLine()!.Trim();

        Console.Write("Language (ex: pt-BR, en-US, es-ES): ");
        string language = Console.ReadLine()!.Trim();

        var serverUri = new Uri("ws://localhost:8080");

        using var ws = new ClientWebSocket();

        Console.WriteLine($"Conectando em {serverUri}...");
        await ws.ConnectAsync(serverUri, CancellationToken.None);
        Console.WriteLine("Conectado!");

        // Envia mensagem de join
        var joinMsg = new
        {
            type = "join",
            clientId,
            roomId,
            language
        };

        await SendJsonAsync(ws, joinMsg);

        // Task separada para ouvir mensagens do servidor
        var cts = new CancellationTokenSource();
        var receiveTask = Task.Run(() => ReceiveLoop(ws, cts.Token));

        Console.WriteLine();
        Console.WriteLine("Digite frases para simular o Speech-to-Text.");
        Console.WriteLine("Linha vazia encerra o cliente.");
        Console.WriteLine("-------------------------------------------------");

        while (ws.State == WebSocketState.Open)
        {
            Console.Write("> ");
            string? text = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(text))
                break;

            var utterance = new
            {
                type = "utterance",
                utteranceId = Guid.NewGuid().ToString(),
                speakerId = clientId,
                roomId = roomId,
                language = language,
                text = text
            };

            await SendJsonAsync(ws, utterance);
        }

        Console.WriteLine("Encerrando conexão...");
        cts.Cancel();
        if (ws.State == WebSocketState.Open)
        {
            await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client exit", CancellationToken.None);
        }
    }

    static async Task SendJsonAsync(ClientWebSocket ws, object obj)
    {
        string json = JsonSerializer.Serialize(obj);
        byte[] bytes = Encoding.UTF8.GetBytes(json);
        var segment = new ArraySegment<byte>(bytes);

        await ws.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None);
    }

    static async Task ReceiveLoop(ClientWebSocket ws, CancellationToken token)
    {
        var buffer = new byte[4096];

        try
        {
            while (!token.IsCancellationRequested && ws.State == WebSocketState.Open)
            {
                var segment = new ArraySegment<byte>(buffer);
                WebSocketReceiveResult result;

                try
                {
                    result = await ws.ReceiveAsync(segment, token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    Console.WriteLine("Servidor fechou a conexão.");
                    break;
                }

                var msg = Encoding.UTF8.GetString(buffer, 0, result.Count);
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine();
                Console.WriteLine($"[SERVER] {msg}");
                Console.ResetColor();
                Console.Write("> ");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro no receive: {ex.Message}");
        }
    }
}
