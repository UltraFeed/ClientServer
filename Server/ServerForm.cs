using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using Crypto;

#pragma warning disable CA1031
#pragma warning disable CA2213
#pragma warning disable CS8618
#pragma warning disable IDE0058

namespace Server;

public partial class ServerForm : Form
{
    private static NetworkStream stream;
    private const int rsaKeySize = 2048;
    private readonly RSACryptoServiceProvider rsa = new(rsaKeySize);
    private readonly Aes aes = Aes.Create();

    public ServerForm ()
    {
        InitializeComponent();
        InitializeNetwork();
    }

    private async void InitializeNetwork ()
    {
        try
        {
            using TcpListener listener = new(IPAddress.Any, 5000);
            listener.Start();
            AppendText("Сервер ожидает подключения...");

            using TcpClient client = await listener.AcceptTcpClientAsync().ConfigureAwait(false);
            stream = client.GetStream();
            AppendText("Клиент подключился");

            if (MessageEncryption.algo == MessageEncryption.Algorithm.RSA)
            {
                aes.Padding = PaddingMode.PKCS7;
                rsa.PersistKeyInCsp = false;
                string publicKey = rsa.ToXmlString(false);
                PublicKey.SendPublicKey(stream, publicKey);
                AppendText($"Открытый ключ отправлен: {publicKey}");
                string logs = PublicKey.ReceiveSessionKey(stream, aes, rsa);
                AppendText(logs);
            }

            await Task.Run(ReceiveMessagesAsync).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            AppendText($"Ошибка подключения: {ex.Message}");
        }
    }

    private async Task ReceiveMessagesAsync ()
    {
        while (true)
        {
            try
            {
                byte [] lengthBuffer = new byte [4];
                int bytesRead = await stream.ReadAsync(lengthBuffer).ConfigureAwait(false);
                if (bytesRead == 0)
                {
                    break;
                }

                int encryptedMessageLength = BitConverter.ToInt32(lengthBuffer);
                byte [] encryptedMessage = new byte [encryptedMessageLength];
                await stream.ReadAsync(encryptedMessage).ConfigureAwait(false);
                byte [] decryptedMessage = Helper.DecryptMessage(encryptedMessage, aes);

                AppendText($"Клиент: {Encoding.UTF8.GetString(decryptedMessage)}");
            }
            catch (Exception ex)
            {
                AppendText($"Ошибка при получении сообщения: {ex.Message}");
                break;
            }
        }
    }

    private async Task SendMessageAsync (string message)
    {
        AppendText($"Вы: {message}");
        byte [] encryptedMessage = Helper.EncryptMessage(Encoding.UTF8.GetBytes(message), aes);
        byte [] lengthBuffer = BitConverter.GetBytes(encryptedMessage.Length);
        await stream.WriteAsync(lengthBuffer).ConfigureAwait(false);
        await stream.WriteAsync(encryptedMessage).ConfigureAwait(false);
    }

    private async void ButtonSend_Click (object? sender, EventArgs e)
    {
        string message = textBoxInput.Text;
        textBoxInput.Clear();
        await SendMessageAsync(message).ConfigureAwait(false);
    }

    private void AppendText (string text)
    {
        if (InvokeRequired)
        {
            Invoke(new Action<string>(AppendText), text);
        }
        else
        {
            textBoxMessages.AppendText(Environment.NewLine + text + Environment.NewLine);
        }
    }
}
