using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Helper;

#pragma warning disable CA1303
#pragma warning disable CA2000
#pragma warning disable CA2213
#pragma warning disable CS8601
#pragma warning disable CS8602
#pragma warning disable CS8604
#pragma warning disable CS8618
#pragma warning disable IDE0058

namespace Client;

public partial class ChatForm : Form
{
    private ClientWebSocket _webSocket;
    private readonly string _username;
    private HashSet<string> _clients = [];
    private ListBox clientsListBox;
    private TextBox messageTextBox;
    private Button sendButton;
    private TextBox chatTextBox;

    public ChatForm (string username)
    {
        _username = username;
        InitializeComponent();
        StartConnection();
    }

    private async void StartConnection ()
    {
        Uri uri = new("ws://localhost:5000/ws/");
        _webSocket = new ClientWebSocket();
        await _webSocket.ConnectAsync(uri, CancellationToken.None).ConfigureAwait(true);
        Invoke(new Action(() => chatTextBox.AppendText($"[Connected to the {uri}]" + Environment.NewLine)));

        // Отправляем юзернейм на сервер
        byte [] usernameBytes = Encoding.UTF8.GetBytes(_username);
        await _webSocket.SendAsync(new ArraySegment<byte>(usernameBytes), WebSocketMessageType.Text, true, CancellationToken.None).ConfigureAwait(false);

        // Начинаем принимать сообщения
        _ = ReceiveMessages();

        // Периодически запрашиваем список клиентов
        System.Windows.Forms.Timer clientUpdateTimer = new() { Interval = 5000 }; // Запрашивать каждые 5 секунд
        clientUpdateTimer.Tick += async (sender, e) => await RequestClientList().ConfigureAwait(false);
        clientUpdateTimer.Start();
    }

    private async Task RequestClientList ()
    {
        if (_webSocket.State == WebSocketState.Open)
        {
            // Отправляем запрос на получение списка активных клиентов
            Helper.Message message = new()
            {
                Type = MessageType.ClientsList,
                SenderUsername = _username,
                Content = ""
            };

            string messageJson = JsonSerializer.Serialize(message);
            byte [] bytes = Encoding.UTF8.GetBytes(messageJson);
            await _webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None).ConfigureAwait(false);
        }
    }

    private async Task ReceiveMessages ()
    {
        byte [] buffer = new byte [1024 * 4];

        while (_webSocket.State == WebSocketState.Open)
        {
            WebSocketReceiveResult result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None).ConfigureAwait(false);
            string messageJson = Encoding.UTF8.GetString(buffer, 0, result.Count);

            //Invoke(new Action(() => chatTextBox.AppendText($"Received message: {messageJson}")));

            // Десериализуем сообщение
            Helper.Message? message = JsonSerializer.Deserialize<Helper.Message>(messageJson);

            if (message != null)
            {
                // Проверяем тип сообщения
                if (message.Type == MessageType.ClientsList)
                {
                    // Сообщение содержит список клиентов
                    _clients.Clear();
                    _clients = JsonSerializer.Deserialize<HashSet<string>>(message.Content); // Десериализация списка клиентов
                    UpdateClientList(); // Обновление списка клиентов в интерфейсе
                }
                else if (message.Type == MessageType.Text)
                {
                    Invoke(new Action(() => chatTextBox.AppendText($"[{message.SenderUsername}]: {message.Content}{Environment.NewLine}")));
                }
            }
        }
    }

    private async void SendButton_Click (object? sender, EventArgs e)
    {
        string selectedClient = clientsListBox.SelectedItem?.ToString() ?? "Broadcast";
        string messageText = messageTextBox.Text;

        if (!string.IsNullOrWhiteSpace(messageText))
        {
            Helper.Message message = new()
            {
                Type = MessageType.Text,
                SenderUsername = _username,
                ReceiverUsername = selectedClient == "Broadcast" ? null : selectedClient,
                Content = messageText
            };

            string messageJson = JsonSerializer.Serialize(message);
            byte [] bytes = Encoding.UTF8.GetBytes(messageJson);
            await _webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None).ConfigureAwait(false);

            Invoke(new Action(() => chatTextBox.AppendText($"[{_username}]: {messageText}" + Environment.NewLine)));

            messageTextBox.Clear();
        }
    }

    private void UpdateClientList ()
    {
        Invoke(new Action(() =>
        {
            // Запоминаем текущий выбранный элемент
            string? previouslySelectedClient = clientsListBox.SelectedItem?.ToString();

            clientsListBox.Items.Clear();
            clientsListBox.Items.Add("Broadcast"); // Добавляем общий чат

            foreach (string client in _clients)
            {
                // Исключаем из списка самого себя
                if (client != _username)
                {
                    clientsListBox.Items.Add(client);
                }
            }

            // Если ранее выбранный элемент все еще есть в списке - выбираем его
            if (!string.IsNullOrEmpty(previouslySelectedClient) && clientsListBox.Items.Contains(previouslySelectedClient))
            {
                clientsListBox.SelectedItem = previouslySelectedClient;
            }
            else
            {
                // Иначе выбираем "Broadcast" как элемент по умолчанию
                clientsListBox.SelectedItem = "Broadcast";
            }
        }));
    }

    private void ChatForm_FormClosing (object? sender, FormClosingEventArgs e)
    {
        _webSocket?.Dispose();
    }

    public static void Main ()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        ChatForm form1 = new("Client1");
        ChatForm form2 = new("Client2");

        form1.Show();
        form2.Show();

        Application.Run();
    }

    private void MessageTextBox_KeyDown (object? sender, KeyEventArgs e)
    {
        // Проверяем, была ли нажата клавиша Enter
        if (e.KeyCode == Keys.Enter)
        {
            // Блокируем стандартное поведение Enter (например, переход на новую строку)
            e.SuppressKeyPress = true;

            // Вызываем обработчик клика кнопки Send
            SendButton_Click(sender, e);
        }
    }

    private void InitializeComponent ()
    {
        // Создание и настройка компонентов
        clientsListBox = new ListBox
        {
            Dock = DockStyle.Fill,
            FormattingEnabled = true
        };

        messageTextBox = new TextBox
        {
            Dock = DockStyle.Fill
        };
        messageTextBox.KeyDown += new KeyEventHandler(MessageTextBox_KeyDown);

        sendButton = new Button
        {
            Text = "Send",
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink
        };
        sendButton.Click += new EventHandler(SendButton_Click);

        chatTextBox = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            Dock = DockStyle.Fill
        };

        // Создание и настройка основного макета
        TableLayoutPanel mainTableLayoutPanel = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink
        };

        mainTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20)); // Для clientsListBox
        mainTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 80)); // Для остальных элементов

        mainTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 80)); // Для chatTextBox
        mainTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Для строки с полем ввода и кнопкой

        mainTableLayoutPanel.Controls.Add(clientsListBox);
        mainTableLayoutPanel.SetRowSpan(clientsListBox, 2); // clientsListBox занимает две строки

        mainTableLayoutPanel.Controls.Add(chatTextBox);

        // Создание и настройка панели ввода
        TableLayoutPanel inputPanel = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink
        };

        inputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 80)); // Поле ввода
        inputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20)); // Кнопка

        inputPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F)); // Высота строки

        inputPanel.Controls.Add(messageTextBox);
        inputPanel.Controls.Add(sendButton);

        // Добавление панели ввода в основной макет
        mainTableLayoutPanel.Controls.Add(inputPanel);

        // Настройка формы
        Controls.Add(mainTableLayoutPanel);
        Text = _username;
        FormClosing += new FormClosingEventHandler(ChatForm_FormClosing);
        MinimumSize = new Size(600, 400);
    }
}
