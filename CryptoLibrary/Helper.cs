using System.Security.Cryptography;

namespace Crypto;

public class Encryption
{
#pragma warning disable CA2211
    public static Algorithm algorithm = Algorithm.RSA;
#pragma warning restore CA2211
    public enum Algorithm
    {
        None,
        RSA
    }
}

public class Helper
{

    public const int rsaKeySize = 2048;
    public readonly RSACryptoServiceProvider rsa = new(rsaKeySize);
    public readonly Aes aes = Aes.Create();

    public byte [] EncryptMessage (byte [] message)
    {
        if (Encryption.algorithm == Encryption.Algorithm.None)
        {
            return message;
        }
        else if (Encryption.algorithm == Encryption.Algorithm.RSA)
        {
            using MemoryStream ms = new();
            using CryptoStream cs = new(ms, aes.CreateEncryptor(), CryptoStreamMode.Write);
            cs.Write(message);
            cs.FlushFinalBlock();
            return ms.ToArray();
        }
        else
        {
            throw new ArgumentException("Algorithm not supported");
        }
    }

    public byte [] DecryptMessage (byte [] message)
    {
        if (Encryption.algorithm == Encryption.Algorithm.None)
        {
            return message;
        }
        else if (Encryption.algorithm == Encryption.Algorithm.RSA)
        {
            using MemoryStream ms = new(message);
            using CryptoStream cs = new(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
            using MemoryStream decryptedStream = new();
            cs.CopyTo(decryptedStream);
            return decryptedStream.ToArray();
        }
        else
        {
            throw new ArgumentException("Algorithm not supported");
        }
    }
}
