/*

using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

#pragma warning disable CA1303
#pragma warning disable CS8618
#pragma warning disable IDE0058

namespace Server;

internal sealed class Server2
{
    private const int rsaKeySize = 2048;
    private static readonly RSACryptoServiceProvider rsa = new(rsaKeySize);
    private static readonly Aes aes = Aes.Create();
    private static NetworkStream stream;

    internal static async Task Main2 ()
    {
        rsa.PersistKeyInCsp = false;
        string publicKey = rsa.ToXmlString(false);

        using TcpListener listener = new(IPAddress.Any, 5000);
        listener.Start();
        Console.WriteLine("Сервер ожидает подключения...");

        using TcpClient client = await listener.AcceptTcpClientAsync().ConfigureAwait(false);
        stream = client.GetStream();

        Console.WriteLine("Клиент подключился.");

        SendPublicKey(stream, publicKey);
        ReceiveSessionKeyAndIV(stream);

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

        await receiveTask.ConfigureAwait(false);
    }

    private static void SendPublicKey (NetworkStream stream, string publicKey)
    {
        byte [] publicKeyBytes = Encoding.UTF8.GetBytes(publicKey);
        stream.Write(publicKeyBytes);
        Console.WriteLine($"Открытый ключ RSA отправлен: {Encoding.UTF8.GetString(publicKeyBytes)}");
    }

    private static void ReceiveSessionKeyAndIV (NetworkStream stream)
    {
        byte [] encryptedSessionKey = new byte [256];
        stream.Read(encryptedSessionKey);

        byte [] sessionKey = rsa.Decrypt(encryptedSessionKey, false);
        Console.WriteLine($"\nСеансовый ключ AES получен: {BitConverter.ToString(encryptedSessionKey)}");
        Console.WriteLine($"\nСеансовый ключ AES расшифрован: {BitConverter.ToString(sessionKey)}");
        aes.Key = sessionKey;

        byte [] iv = new byte [aes.BlockSize / 8];
        stream.Read(iv);
        aes.IV = iv;

        Console.WriteLine("\nСеансовый ключ и IV получены и расшифрованы.");
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
                Console.WriteLine($"Получено сообщение от клиента: {Encoding.UTF8.GetString(decryptedMessage)}");
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

    private static byte [] EncryptMessage (byte [] message, byte [] key, byte [] iv)
    {
        using Aes aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        aes.Padding = PaddingMode.PKCS7;

        using MemoryStream ms = new();
        using CryptoStream cs = new(ms, aes.CreateEncryptor(), CryptoStreamMode.Write);
        cs.Write(message, 0, message.Length);
        cs.FlushFinalBlock();
        return ms.ToArray();
    }
}

*/