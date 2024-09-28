using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Helper;

#pragma warning disable CA1031
#pragma warning disable CA1303
#pragma warning disable CS8629
#pragma warning disable IDE0058

namespace Server;

internal sealed class Server
{
    private static readonly ConcurrentDictionary<WebSocket, ClientInfo> _clients = new();
    private static readonly ConcurrentDictionary<string, string> _usernameToId = new();
    private static int _clientIdCounter = 1;

    private static async Task HandleClient (WebSocket webSocket)
    {
        byte [] buffer = new byte [1024 * 4];

        // Присваиваем ID клиенту
        string clientId = _clientIdCounter++.ToString(CultureInfo.InvariantCulture);

        // Получаем юзернейм
        WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None).ConfigureAwait(false);
        string username = Encoding.UTF8.GetString(buffer, 0, result.Count);
        _usernameToId [username] = clientId;
        _clients [webSocket] = new ClientInfo { Id = clientId, Username = username };

        Console.WriteLine($"Client {clientId} with username '{username}' connected.");

        while (!result.CloseStatus.HasValue)
        {
            try
            {
                result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None).ConfigureAwait(false);
                if (result.CloseStatus.HasValue)
                {
                    break;
                }

                string messageJson = Encoding.UTF8.GetString(buffer, 0, result.Count);
                Helper.Message? message = JsonSerializer.Deserialize<Helper.Message>(messageJson);

                if (message == null)
                {
                    continue;
                }

                if (message.Type == MessageType.ClientsList)
                {
                    // Клиент запросил список всех активных пользователей
                    Helper.Message clientsListMessage = new()
                    {
                        Type = MessageType.ClientsList,
                        SenderUsername = "Server",
                        Content = JsonSerializer.Serialize(_clients.Values.Select(c => c.Username).ToList()) // Сериализация списка клиентов
                    };

                    // Отправляем это сообщение клиентам
                    string clientsListJson = JsonSerializer.Serialize(clientsListMessage);
                    byte [] bytes = Encoding.UTF8.GetBytes(clientsListJson);
                    if (webSocket.State == WebSocketState.Open)
                    {
                        await webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None).ConfigureAwait(false);
                    }
                }
                else if (message.Type is MessageType.Text or MessageType.Key)
                {
                    Console.WriteLine($"Message received from {message.SenderUsername} to {message.ReceiverUsername}: {message.Content}");

                    // Определяем получателя по юзернейму
                    if (message.ReceiverUsername == null) // Broadcast
                    {
                        foreach (KeyValuePair<WebSocket, ClientInfo> client in _clients)
                        {
                            if (client.Key != webSocket && client.Key.State == WebSocketState.Open)
                            {
                                await client.Key.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(messageJson)), result.MessageType, result.EndOfMessage, CancellationToken.None).ConfigureAwait(false);
                            }
                        }
                    }
                    else
                    {
                        WebSocket? receiverSocket = GetClientByUsername(message.ReceiverUsername);
                        if (receiverSocket != null && receiverSocket.State == WebSocketState.Open)
                        {
                            await receiverSocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(messageJson)), result.MessageType, result.EndOfMessage, CancellationToken.None).ConfigureAwait(false);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                break;
            }
        }

        try
        {
            if (webSocket.State == WebSocketState.Open)
            {
                await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error closing WebSocket: {ex.Message}");
        }

        // Удаляем клиента из _clients и _usernameToId
        if (_clients.TryRemove(webSocket, out ClientInfo? clientInfo))
        {
            Console.WriteLine($"Client {clientInfo.Id} with username '{clientInfo.Username}' disconnected");
            _usernameToId.TryRemove(clientInfo.Username, out _);
        }
    }

    private static WebSocket? GetClientByUsername (string username)
    {
        foreach (KeyValuePair<WebSocket, ClientInfo> client in _clients)
        {
            if (client.Value.Username == username)
            {
                return client.Key;
            }
        }

        return null;
    }

    public static async Task StartServer ()
    {
        using HttpListener httpListener = new();
        httpListener.Prefixes.Add("http://localhost:5000/ws/");
        httpListener.Start();
        Console.WriteLine("WebSocket server started at ws://localhost:5000/ws/");

        while (true)
        {
            HttpListenerContext listenerContext = await httpListener.GetContextAsync().ConfigureAwait(false);

            if (listenerContext.Request.IsWebSocketRequest)
            {
                HttpListenerWebSocketContext webSocketContext = await listenerContext.AcceptWebSocketAsync(null).ConfigureAwait(false);
                WebSocket webSocket = webSocketContext.WebSocket;
                _ = HandleClient(webSocket);
            }
        }
    }

    public static void Main ()
    {
        StartServer().GetAwaiter().GetResult();
    }
}
