using System.Security.Cryptography;

#pragma warning disable CA1051

namespace Crypto;

public static class Encryption
{
#pragma warning disable CA2211
    public static Algorithm algo = Algorithm.RSA;
#pragma warning restore CA2211
    public enum Algorithm
    {
        None,
        RSA
    }
}

public class Helper : IDisposable
{

    public const int rsaKeySize = 2048;
    public readonly RSACryptoServiceProvider rsa = new(rsaKeySize);
    public readonly Aes aes = Aes.Create();

    public byte [] EncryptMessage (byte [] message)
    {
        if (Encryption.algo == Encryption.Algorithm.None)
        {
            return message;
        }
        else if (Encryption.algo == Encryption.Algorithm.RSA)
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
        if (Encryption.algo == Encryption.Algorithm.None)
        {
            return message;
        }
        else if (Encryption.algo == Encryption.Algorithm.RSA)
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

    public void Dispose ()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose (bool disposing)
    {
        if (disposing)
        {
            rsa.Dispose();
            aes.Dispose();
        }
    }

    ~Helper ()
    {
        Dispose(false);
    }
}
