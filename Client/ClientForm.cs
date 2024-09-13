using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using Crypto;

#pragma warning disable CA1031
#pragma warning disable CA2213
#pragma warning disable CS8618
#pragma warning disable IDE0058

namespace Client;

public partial class ClientForm : Form
{
    private static NetworkStream stream;
    private const int rsaKeySize = 2048;
    private readonly RSACryptoServiceProvider rsa = new(rsaKeySize);
    private readonly Aes aes = Aes.Create();

    public ClientForm (string name)
    {
        InitializeComponent(name);
        InitializeNetwork();
    }

    private async void InitializeNetwork ()
    {
        try
        {
            AppendText("Подключение к серверу");
            using TcpClient client = new("localhost", 5000);

            stream = client.GetStream();
            AppendText("Подключение к серверу выполнено");

            if (MessageEncryption.algo == MessageEncryption.Algorithm.RSA)
            {
                aes.Padding = PaddingMode.PKCS7;
                rsa.PersistKeyInCsp = false;
                string publicKey = PublicKey.ReceivePublicKey(stream, rsaKeySize);
                AppendText($"Получен открытый ключ: {publicKey}");
                string logs = PublicKey.SendSessionKey(stream, aes, rsa, publicKey);
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
                AppendText($"Сервер: {Encoding.UTF8.GetString(decryptedMessage)}");
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
