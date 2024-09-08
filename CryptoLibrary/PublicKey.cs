using System.Net.Sockets;
using System.Text;

#pragma warning disable CA1062
#pragma warning disable CA1305
#pragma warning disable IDE0058

namespace Crypto;

public static class KeyEncryption
{
#pragma warning disable CA2211
    public static Algorithm algo = Algorithm.AES;
#pragma warning restore CA2211
    public enum Algorithm
    {
        AES,
        Elgamal
    }
}
public static class PublicKey
{
    public static string ReceiveSessionKey (NetworkStream stream, Helper helper)
    {
        StringBuilder logs = new();
        if (KeyEncryption.algo == KeyEncryption.Algorithm.AES)
        {
            byte [] encryptedSessionKey = new byte [256];
            stream.Read(encryptedSessionKey);
            byte [] sessionKey = helper.rsa.Decrypt(encryptedSessionKey, false);
            helper.aes.Key = sessionKey;
            byte [] iv = new byte [helper.aes.BlockSize / 8];
            stream.Read(iv);
            helper.aes.IV = iv;

            logs.AppendLine($"Зашифрованный cеансовый ключ получен: {BitConverter.ToString(encryptedSessionKey)}");
            logs.AppendLine(Environment.NewLine + $"Сеансовый ключ расшифрован: {BitConverter.ToString(sessionKey)}");
            return logs.ToString();
        }
        else
        {
            throw new ArgumentException("Unknown Key encryption algorithm");
        }
    }

    public static string SendSessionKey (NetworkStream stream, Helper helper, string publicKey)
    {
        StringBuilder logs = new();
        if (KeyEncryption.algo == KeyEncryption.Algorithm.AES)
        {
            helper.rsa.FromXmlString(publicKey);
            helper.aes.GenerateKey();
            helper.aes.GenerateIV();

            byte [] sessionKey = helper.aes.Key;
            byte [] iv = helper.aes.IV;

            byte [] encryptedSessionKey = helper.rsa.Encrypt(sessionKey, false);
            stream.Write(encryptedSessionKey);

            logs.AppendLine($"Сеансовый ключ создан: {BitConverter.ToString(sessionKey)}");
            logs.AppendLine(Environment.NewLine + $"Сеансовый ключ зашифрован и отправлен: {BitConverter.ToString(encryptedSessionKey)}");
            stream.Write(iv);
            return logs.ToString();
        }
        else
        {
            throw new ArgumentException("Unknown Key encryption algorithm");
        }
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
