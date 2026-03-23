using  ChatClient.Services.Models;

namespace  ChatClient.Services;

public interface IChatService
{
    /// <summary>
    /// Streams tokens from Azure AI Foundry and calls onToken for each chunk.
    /// Returns the full assembled response.
    /// </summary>
    IAsyncEnumerable<string> StreamResponseAsync(
        IEnumerable<ChatMessage> history,
        CancellationToken cancellationToken = default);
}
