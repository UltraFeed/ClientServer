#pragma warning disable CA1303
#pragma warning disable CA2213
#pragma warning disable IDE0058

namespace Server;

public partial class ServerForm
{
    private TextBox textBoxMessages;
    private TextBox textBoxInput;
    private Button buttonSend;

    private void InitializeComponent ()
    {
        AutoScaleMode = AutoScaleMode.Dpi;
        FormBorderStyle = FormBorderStyle.Sizable;
        Text = "Server";

        TableLayoutPanel mainLayout = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2
        };

        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 80)); // Окно с логами - 80% высоты
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 20)); // Панель ввода текста и кнопки - 20% высоты

        textBoxMessages = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            TabIndex = 1,
            ScrollBars = ScrollBars.Vertical
        };

        TableLayoutPanel inputPanel = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1
        };

        inputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 80)); // Поле ввода текста - 80% ширины
        inputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20)); // Кнопка - 20% ширины

        textBoxInput = new TextBox
        {
            Dock = DockStyle.Fill,
            TabIndex = 0
        };

        buttonSend = new Button
        {
            Dock = DockStyle.Fill,
            TabIndex = 2,
            Text = "Send",
            UseVisualStyleBackColor = true
        };

        inputPanel.Controls.Add(textBoxInput, 0, 0);
        inputPanel.Controls.Add(buttonSend, 1, 0);

        mainLayout.Controls.Add(textBoxMessages, 0, 0);
        mainLayout.Controls.Add(inputPanel, 0, 1);

        Controls.Add(mainLayout);

        ClientSize = new Size(700, 500);
        MinimumSize = new Size(300, 200);

        FormClosing += OnFormClosing;
        buttonSend.Click += ButtonSend_Click;
        textBoxInput.KeyDown += TextBoxInput_KeyDown;
    }
}
