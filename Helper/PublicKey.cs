using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;

#pragma warning disable CA1062
#pragma warning disable CA1305
#pragma warning disable IDE0058
#pragma warning disable IDE0305

namespace Helper;

public static class PublicKey
{
    public static async Task<string> ReceiveSessionKeyAsync (WebSocket webSocket, Aes aes, RSACryptoServiceProvider rsa)
    {
        StringBuilder logs = new();
        byte [] encryptedSessionKey = new byte [256];
        await ReceiveDataAsync(webSocket, encryptedSessionKey).ConfigureAwait(false);
        byte [] sessionKey = rsa.Decrypt(encryptedSessionKey, false);
        aes.Key = sessionKey;
        byte [] iv = new byte [aes.BlockSize / 8];
        await ReceiveDataAsync(webSocket, iv).ConfigureAwait(false);
        aes.IV = iv;

        // Генерация соли на основе текущего времени
        string salt = DateTime.UtcNow.ToString("yyyyMMddHHmm"); // Точность до минут
        byte [] saltBytes = Encoding.UTF8.GetBytes(salt);

        // Генерация соленого SHA256 хэша
        byte [] sessionKeyWithSalt = sessionKey.Concat(saltBytes).ToArray();
        byte [] hash = SHA256.HashData(sessionKeyWithSalt);

        // Отправка SHA256 хэша клиенту
        await SendDataAsync(webSocket, hash).ConfigureAwait(false);

        logs.AppendLine($"Зашифрованный сеансовый ключ получен: {BitConverter.ToString(encryptedSessionKey)}");
        logs.AppendLine($"Сеансовый ключ расшифрован: {BitConverter.ToString(sessionKey)}");
        logs.AppendLine($"Соль сгенерирована: {salt}");
        logs.AppendLine($"Соленый SHA256 хэш отправлен: {BitConverter.ToString(hash)}");

        return logs.ToString();
    }

    public static async Task<string> SendSessionKeyAsync (WebSocket webSocket, Aes aes, RSACryptoServiceProvider rsa, string publicKey)
    {
        StringBuilder logs = new();
        rsa.FromXmlString(publicKey);
        aes.GenerateKey();
        aes.GenerateIV();

        byte [] sessionKey = aes.Key;
        byte [] iv = aes.IV;

        byte [] encryptedSessionKey = rsa.Encrypt(sessionKey, false);
        await SendDataAsync(webSocket, encryptedSessionKey).ConfigureAwait(false);

        logs.AppendLine($"Сеансовый ключ создан: {BitConverter.ToString(sessionKey)}");
        logs.AppendLine($"Сеансовый ключ зашифрован и отправлен: {BitConverter.ToString(encryptedSessionKey)}");
        await SendDataAsync(webSocket, iv).ConfigureAwait(false);

        // Использование соли на основе времени
        string salt = DateTime.UtcNow.ToString("yyyyMMddHHmm"); // Точность до минут
        logs.AppendLine($"Соль сгенерирована: {salt}");

        byte [] saltBytes = Encoding.UTF8.GetBytes(salt);

        // Получение SHA256 хэша от клиента
        byte [] receivedHash = new byte [32]; // 32 байта для SHA256
        await ReceiveDataAsync(webSocket, receivedHash).ConfigureAwait(false);
        logs.AppendLine($"SHA256 хэш получен: {BitConverter.ToString(receivedHash)}");

        // Генерация собственного соленого SHA256 хэша для сравнения
        byte [] sessionKeyWithSalt = sessionKey.Concat(saltBytes).ToArray();
        byte [] computedHash = SHA256.HashData(sessionKeyWithSalt);

        if (!computedHash.SequenceEqual(receivedHash))
        {
            logs.AppendLine("Ошибка проверки подлинности: Хэши не совпадают!");
        }
        else
        {
            logs.AppendLine("Проверка подлинности успешна: Хэши совпадают.");
        }

        return logs.ToString();
    }

    public static async Task SendPublicKeyAsync (WebSocket webSocket, string publicKey)
    {
        byte [] publicKeyBytes = Encoding.UTF8.GetBytes(publicKey);
        await SendDataAsync(webSocket, publicKeyBytes).ConfigureAwait(false);
    }

    public static async Task<string> ReceivePublicKeyAsync (WebSocket webSocket, int rsaKeySize)
    {
        byte [] publicKeyBytes = new byte [rsaKeySize];
        await ReceiveDataAsync(webSocket, publicKeyBytes).ConfigureAwait(false);
        string publicKey = Encoding.UTF8.GetString(publicKeyBytes);
        return publicKey;
    }

    private static async Task SendDataAsync (WebSocket webSocket, byte [] data)
    {
        ArraySegment<byte> segment = new(data);
        await webSocket.SendAsync(segment, WebSocketMessageType.Binary, true, CancellationToken.None).ConfigureAwait(false);
    }

    private static async Task ReceiveDataAsync (WebSocket webSocket, byte [] buffer)
    {
        ArraySegment<byte> segment = new(buffer);
        WebSocketReceiveResult result = await webSocket.ReceiveAsync(segment, CancellationToken.None).ConfigureAwait(false);
        if (result.MessageType == WebSocketMessageType.Close)
        {
            throw new WebSocketException("Connection closed by the remote host.");
        }
    }
}

