/*

using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

#pragma warning disable CA1303
#pragma warning disable CS8618
#pragma warning disable IDE0058

namespace Client;

internal sealed class Client2
{
    private const int rsaKeySize = 2048;
    private static readonly Aes aes = Aes.Create();
    private static NetworkStream stream;

    internal static async Task Main2 ()
    {
        using TcpClient client = new("localhost", 5000);
        stream = client.GetStream();

        string publicKey = ReceivePublicKey(stream);
        SendSessionKeyAndIV(stream, publicKey);

        Console.WriteLine("\nСеансовый ключ и IV сгенерированы и отправлены серверу.");

        Task receiveTask = Task.Run(ReceiveMessagesAsync);

        while (true)
        {
            Console.WriteLine("Введите сообщение для отправки серверу или 'exit' для выхода:");
            string input = Console.ReadLine();

            if (input?.ToUpperInvariant() == "EXIT")
            {
                break;
            }

            await SendMessageAsync(input).ConfigureAwait(false);
        }

        // Дожидаемся завершения фоновой задачи
        await receiveTask.ConfigureAwait(false);
    }

    private static string ReceivePublicKey (NetworkStream stream)
    {
        byte [] publicKeyBytes = new byte [rsaKeySize];
        int bytesRead = stream.Read(publicKeyBytes);
        string publicKey = Encoding.UTF8.GetString(publicKeyBytes, 0, bytesRead);
        Console.WriteLine($"Открытый ключ RSA получен: {publicKey}");
        return publicKey;
    }

    private static void SendSessionKeyAndIV (NetworkStream stream, string publicKey)
    {
        using RSACryptoServiceProvider rsa = new();
        rsa.FromXmlString(publicKey);

        aes.GenerateKey();
        byte [] sessionKey = aes.Key;
        aes.GenerateIV();
        byte [] iv = aes.IV;

        byte [] encryptedSessionKey = rsa.Encrypt(sessionKey, false);
        Console.WriteLine($"\nСеансовый ключ AES создан: {BitConverter.ToString(sessionKey)}");
        Console.WriteLine($"\nСеансовый ключ AES зашифрован и отправлен: {BitConverter.ToString(encryptedSessionKey)}");
        stream.Write(encryptedSessionKey);

        stream.Write(iv);
    }

    private static async Task ReceiveMessagesAsync ()
    {
        while (true)
        {
            try
            {
                byte [] lengthBuffer = new byte [4];
                int bytesRead = await stream.ReadAsync(lengthBuffer).ConfigureAwait(false);
                if (bytesRead == 0)
                {
                    break; // Соединение прервано
                }

                int encryptedMessageLength = BitConverter.ToInt32(lengthBuffer);
                byte [] encryptedMessage = new byte [encryptedMessageLength];
                await stream.ReadAsync(encryptedMessage).ConfigureAwait(false);

                byte [] decryptedMessage = DecryptMessage(encryptedMessage, aes.Key, aes.IV);
                Console.WriteLine($"Получено сообщение от сервера: {Encoding.UTF8.GetString(decryptedMessage)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при получении сообщения: {ex.Message}");
                break;
            }
        }
    }

    private static async Task SendMessageAsync (string message)
    {
        byte [] encryptedMessage = EncryptMessage(Encoding.UTF8.GetBytes(message), aes.Key, aes.IV);

        byte [] lengthBuffer = BitConverter.GetBytes(encryptedMessage.Length);
        await stream.WriteAsync(lengthBuffer).ConfigureAwait(false);
        await stream.WriteAsync(encryptedMessage).ConfigureAwait(false);
    }

    private static byte [] EncryptMessage (byte [] message, byte [] key, byte [] iv)
    {
        using Aes aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        aes.Padding = PaddingMode.PKCS7;

        using MemoryStream ms = new();
        using CryptoStream cs = new(ms, aes.CreateEncryptor(), CryptoStreamMode.Write);
        cs.Write(message);
        cs.FlushFinalBlock();
        return ms.ToArray();
    }

    private static byte [] DecryptMessage (byte [] encryptedMessage, byte [] key, byte [] iv)
    {
        using Aes aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        aes.Padding = PaddingMode.PKCS7;

        using MemoryStream ms = new(encryptedMessage);
        using CryptoStream cs = new(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
        using MemoryStream decryptedStream = new();
        cs.CopyTo(decryptedStream);
        return decryptedStream.ToArray();
    }
}

*/