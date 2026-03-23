# BlazorAIChat

Blazor Server chat client with streaming responses via **Azure AI Foundry** and **MudBlazor** UI.

## Stack

| Concern | Library |
|---|---|
| UI Framework | Blazor Server (.NET 9) |
| Component Library | MudBlazor 7 |
| AI Integration | Azure.AI.Projects + Azure.AI.OpenAI |
| Auth | DefaultAzureCredential (Azure.Identity) |
| Markdown rendering | Markdig |

## Prerequisites

- .NET 9 SDK
- An Azure AI Foundry project with a deployed chat model (e.g. `gpt-4o-mini`)
- `az login` done locally (for DefaultAzureCredential)

## Configuration

Set these via **user-secrets** (recommended for local dev):
weather site [Weather API ](https://www.visualcrossing.com/weather-api)

```bash
dotnet user-secrets init
dotnet user-secrets set "AzureAI:ProjectEndpoint" "https://<RESOURCE>.services.ai.azure.com/api/projects/<PROJECT>"
dotnet user-secrets set "AzureAI:ModelDeploymentName" "gpt-41"
dotnet user-secrets set "AzureAI:ApiKey" "<api key>"
dotnet user-secrets set "WeatherApi:ApiKey" "L"
```

Or via environment variables in production:
```
AzureAI__ProjectEndpoint
AzureAI__ModelDeploymentName
```

## Run

```bash
dotnet run
```

## Scaling to production

Add **Azure SignalR Service** backplane to remove sticky-session constraints:

```bash
dotnet add package Microsoft.Azure.SignalR
```

```csharp
// Program.cs
builder.Services.AddSignalR().AddAzureSignalR(config["Azure:SignalRConnectionString"]);
```

Then deploy to Azure App Service with multiple instances — no sticky sessions needed.

## Project structure

```
BlazorAIChat/
├── Components/
│   ├── App.razor
│   ├── Routes.razor
│   ├── Layout/
│   │   └── MainLayout.razor       # MudBlazor shell + theme
│   └── Pages/
│       └── Chat.razor             # Main chat UI with streaming
├── Models/
│   └── ChatMessage.cs             # Message model
├── Services/
│   ├── IChatService.cs            # Streaming interface
│   └── FoundryChatService.cs      # Azure AI Foundry implementation
└── wwwroot/
    ├── app.css                    # Markdown + layout styles
    └── interop.js                 # JS scroll helper
```
