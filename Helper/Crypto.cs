#pragma warning disable CA1062

using System.Security.Cryptography;

namespace Helper;

public static class MessageEncryption
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

public static class Crypto
{

	public static byte [] EncryptMessage (byte [] message, Aes aes)
	{
		if (MessageEncryption.algo == MessageEncryption.Algorithm.None)
		{
			return message;
		}
		else if (MessageEncryption.algo == MessageEncryption.Algorithm.RSA)
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

	public static byte [] DecryptMessage (byte [] message, Aes aes)
	{
		if (MessageEncryption.algo == MessageEncryption.Algorithm.None)
		{
			return message;
		}
		else if (MessageEncryption.algo == MessageEncryption.Algorithm.RSA)
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
