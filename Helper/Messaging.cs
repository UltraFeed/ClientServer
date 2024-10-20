namespace Helper;

public class ClientInfo
{
	public required string Id { get; set; }
	public required string Username { get; set; }
}

public class Message
{
	public required MessageType Type { get; set; } // Тип сообщения, например text, key
	public required string SenderUsername { get; set; } // Юзернейм отправителя
	public string? ReceiverUsername { get; set; } // Юзернейм получателя, может быть null для broadcast
	public string? Content { get; set; } // Содержание сообщения
}

public enum MessageType
{
	ClientsList,
	Text,
	Key
}

