namespace  ChatClient.Services.Models;

public enum MessageRole { User, Assistant, System }

public class ChatMessage
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public MessageRole Role { get; init; }
    public string Content { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    public bool IsStreaming { get; set; }
}
