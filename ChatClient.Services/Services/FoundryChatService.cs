using System.Runtime.CompilerServices;
using Azure.AI.Projects;
using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using System.ClientModel.Primitives;
using Microsoft.Extensions.Configuration;
using ChatClient.Services.Models;
using OpenAI.Chat;
using System.ClientModel;
using OpenAI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using System.ComponentModel;

namespace  ChatClient.Services;

/// <summary>
/// Streams chat completions via Azure AI Foundry using AIProjectClient.
/// Auth: DefaultAzureCredential (managed identity in Azure, az login locally).
/// </summary>
public sealed class FoundryChatService : IChatService
{
    private readonly OpenAIClient _openAIClient;

    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<FoundryChatService> _logger;
    private readonly IWeatherService _weatherService;
    private readonly string _systemPrompt;
    private readonly string _apiKey;

    private string _deploymentName;

    public FoundryChatService(ILoggerFactory loggerFactory,
        AIProjectClient projectClient,
        IWeatherService weatherService,
        IConfiguration config)
    {
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<FoundryChatService>();
        _deploymentName = config["AzureAI:ModelDeploymentName"]
            ?? throw new InvalidOperationException("AzureAI:ModelDeploymentName is not configured.");

        _systemPrompt = config["AzureAI:SystemPrompt"]
            ??
            "You are a helpful assistant. Be concise and clear. You have access to tools to help with user queries. When the user asks about the weather or weather-related questions (in any language, including Dutch), you MUST use the GetWeather tool to provide accurate weather information.  For queries like 'het weer in [location]', extract [location] and call GetWeather with that location. If you are unable to anwer the question use the reverse tool and return the reversed string  of the location. If you cannot extra a location ask for a location. [ Example] What is the weather in Amersfoort? -> calls GetWeather('Amersfoort') and returns the result. [Example] What is the weather in [some location]? -> calls GetWeather('[some location]') and returns the result. [Example] What is the weather like? -> asks the user for a location to get the weather for. Always use the tools when relevant, do not try to answer weather questions without using the tool.";
        
        _apiKey = config["AzureAI:ApiKey"]
            ?? throw new InvalidOperationException("AzureAI:ApiKey is not configured.");

        _weatherService = weatherService;

        // Get the AzureOpenAIClient from the Foundry project connection
        var connectionName = config["AzureAI:ConnectionName"]; // optional, uses default if null
        ClientConnection connection = string.IsNullOrEmpty(connectionName)
            ? projectClient.GetConnection(typeof(AzureOpenAIClient).FullName!)
            : projectClient.GetConnection(connectionName);

        if (!connection.TryGetLocatorAsUri(out Uri? uri) || uri is null)
            throw new InvalidOperationException("Could not resolve Azure OpenAI URI from Foundry project.");

        // Strip path — only the host endpoint is needed
        var endpointUri = new Uri($"https://{uri.Host}");
        var credential = new ApiKeyCredential(_apiKey);
        Console.WriteLine($"Connecting to Azure OpenAI at {endpointUri} with deployment '{_deploymentName}'");
        _openAIClient = new AzureOpenAIClient(endpointUri, credential);
        
    }

    public async IAsyncEnumerable<string> StreamResponseAsync(
        IEnumerable<Models.ChatMessage> history,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {


       // VectorStore vectorStore = new InMemoryVectorStore(new InMemoryVectorStoreOptions()
        {/*
      EmbeddingGenerator = _openAIClient
        .GetEmbeddingClient(embeddingDeploymentName)
        .AsIEmbeddingGenerator()
});*/
            AITool[] tools = new AITool[]
            {
                AIFunctionFactory.Create(
                    Reverse, "Reverse", "Reverses a given string."),
          /*   AIFunctionFactory.Create(
                _weatherService.GetWeather, "GetWeather", "Gets the weather for a specified location.")
 */            };
            

            // Create the chat client and agent, and provide the function tool to the agent.
            // WARNING: DefaultAzureCredential is convenient for development but requires careful consideration in production.
            // In production, consider using a specific credential (e.g., ManagedIdentityCredential) to avoid
            // latency issues, unintended credential probing, and potential security risks from fallback mechanisms.
            AIAgent weatherAgent = _openAIClient
                 .GetChatClient(_deploymentName)
                 .AsAIAgent(
                    instructions: _systemPrompt,
                    name: "WeatherAgent",
        
                    description: "An agent that answers questions about the weather.",
                    tools: [AIFunctionFactory.Create(GetWeather)]);

            var session = await weatherAgent.CreateSessionAsync(cancellationToken);

            var messages = BuildMessages(history);
            _logger.LogInformation("Starting a new chat session with {MessageCount} messages in history.", messages.Count);

            var streamingResult = weatherAgent.RunStreamingAsync(messages, session, cancellationToken: cancellationToken);

            await foreach (var update in streamingResult.WithCancellation(cancellationToken))
            {
                foreach (var part in update.ContentUpdate)
                {
                    if (!string.IsNullOrEmpty(part.Text))
                        yield return part.Text;
                }
            }
        }
    }
    
    [Description("Get the weather for a given location.")]
    public string GetWeather([Description("The location to get the weather for.")] string location)
    => _weatherService.GetWeather(location);


    [Description("Given a string, return the reverse of that string")]
        public string Reverse([Description("The string to be reversed")] string input)
    => $"String reversed";

    private List<OpenAI.Chat.ChatMessage> BuildMessages(IEnumerable<Models.ChatMessage> history)
    {
        var messages = new List<OpenAI.Chat.ChatMessage>
        {
            new SystemChatMessage(_systemPrompt)
        };

        foreach (var msg in history)
        {
            messages.Add(msg.Role switch
            {
                MessageRole.User => new UserChatMessage(msg.Content),
                MessageRole.Assistant => new AssistantChatMessage(msg.Content),
                _ => new UserChatMessage(msg.Content)
            });
        }

        return messages;
    }
}
