using System.Net.Sockets;
using System.Text;

#pragma warning disable IDE0058

namespace Crypto;
public static class PublicKey
{
    public static string ReceiveSessionKeyAndIV (NetworkStream stream, Helper helper)
    {
        byte [] encryptedSessionKey = new byte [256];
        stream.Read(encryptedSessionKey);
        byte [] sessionKey = helper.rsa.Decrypt(encryptedSessionKey, false);
        helper.aes.Key = sessionKey;
        byte [] iv = new byte [helper.aes.BlockSize / 8];
        stream.Read(iv);
        helper.aes.IV = iv;

        StringBuilder logs = new();
        logs.AppendLine($"Зашифрованный cеансовый ключ получен: {BitConverter.ToString(encryptedSessionKey)}");
        logs.AppendLine(Environment.NewLine + $"Сеансовый ключ расшифрован: {BitConverter.ToString(sessionKey)}");
        return logs.ToString();
    }

    public static string SendSessionKeyAndIV (NetworkStream stream, Helper helper, string publicKey)
    {
        helper.rsa.FromXmlString(publicKey);
        helper.aes.GenerateKey();
        helper.aes.GenerateIV();

        byte [] sessionKey = helper.aes.Key;
        byte [] iv = helper.aes.IV;

        byte [] encryptedSessionKey = helper.rsa.Encrypt(sessionKey, false);
        stream.Write(encryptedSessionKey);

        StringBuilder logs = new();
        logs.AppendLine($"Сеансовый ключ создан: {BitConverter.ToString(sessionKey)}");
        logs.AppendLine(Environment.NewLine + $"Сеансовый ключ зашифрован и отправлен: {BitConverter.ToString(encryptedSessionKey)}");
        stream.Write(iv);
        return logs.ToString();
    }

    public static void SendPublicKey (NetworkStream stream, string publicKey)
    {
        byte [] publicKeyBytes = Encoding.UTF8.GetBytes(publicKey);
        stream.Write(publicKeyBytes);
    }

    public static string ReceivePublicKey (NetworkStream stream)
    {
        byte [] publicKeyBytes = new byte [Crypto.Helper.rsaKeySize];
        int bytesRead = stream.Read(publicKeyBytes);
        string publicKey = Encoding.UTF8.GetString(publicKeyBytes, 0, bytesRead);
        return publicKey;
    }
}
