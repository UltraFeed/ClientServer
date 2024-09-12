using System.Collections.Generic;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

#pragma warning disable CA1062
#pragma warning disable CA1305
#pragma warning disable IDE0058
#pragma warning disable IDE0305

namespace Crypto;

public static class PublicKey
{
    public static string ReceiveSessionKey (NetworkStream stream, Helper helper)
    {
        StringBuilder logs = new();
        byte [] encryptedSessionKey = new byte [256];
        stream.Read(encryptedSessionKey);
        byte [] sessionKey = helper.rsa.Decrypt(encryptedSessionKey, false);
        helper.aes.Key = sessionKey;
        byte [] iv = new byte [helper.aes.BlockSize / 8];
        stream.Read(iv);
        helper.aes.IV = iv;

        // Генерация соли на основе текущего времени
        string salt = DateTime.UtcNow.ToString("yyyyMMddHHmm"); // Точность до минут
        byte [] saltBytes = Encoding.UTF8.GetBytes(salt);

        // Генерация соленого SHA256 хэша
        byte [] sessionKeyWithSalt = sessionKey.Concat(saltBytes).ToArray();
        byte [] hash = SHA256.HashData(sessionKeyWithSalt);

        // Отправка SHA256 хэша клиенту
        stream.Write(hash);

        logs.AppendLine($"Зашифрованный cеансовый ключ получен: {BitConverter.ToString(encryptedSessionKey)}");
        logs.AppendLine($"Сеансовый ключ расшифрован: {BitConverter.ToString(sessionKey)}");
        logs.AppendLine($"Соль сгенерирована: {salt}");
        logs.AppendLine($"Соленый SHA256 хэш отправлен: {BitConverter.ToString(hash)}");

        return logs.ToString();
    }

    public static string SendSessionKey (NetworkStream stream, Helper helper, string publicKey)
    {
        StringBuilder logs = new();
        helper.rsa.FromXmlString(publicKey);
        helper.aes.GenerateKey();
        helper.aes.GenerateIV();

        byte [] sessionKey = helper.aes.Key;
        byte [] iv = helper.aes.IV;

        byte [] encryptedSessionKey = helper.rsa.Encrypt(sessionKey, false);
        stream.Write(encryptedSessionKey);

        logs.AppendLine($"Сеансовый ключ создан: {BitConverter.ToString(sessionKey)}");
        logs.AppendLine($"Сеансовый ключ зашифрован и отправлен: {BitConverter.ToString(encryptedSessionKey)}");
        stream.Write(iv);

        // Использование соли на основе времени
        string salt = DateTime.UtcNow.ToString("yyyyMMddHHmm"); // Точность до минут
        logs.AppendLine($"Соль сгенерирована: {salt}");

        byte [] saltBytes = Encoding.UTF8.GetBytes(salt);

        // Получение SHA256 хэша от сервера
        byte [] receivedHash = new byte [32]; // 32 байта для SHA256
        stream.Read(receivedHash);
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

    public static void SendPublicKey (NetworkStream stream, string publicKey)
    {
        byte [] publicKeyBytes = Encoding.UTF8.GetBytes(publicKey);
        stream.Write(publicKeyBytes);
    }

    public static string ReceivePublicKey (NetworkStream stream)
    {
        byte [] publicKeyBytes = new byte [Helper.rsaKeySize];
        int bytesRead = stream.Read(publicKeyBytes);
        string publicKey = Encoding.UTF8.GetString(publicKeyBytes, 0, bytesRead);
        return publicKey;
    }
}
