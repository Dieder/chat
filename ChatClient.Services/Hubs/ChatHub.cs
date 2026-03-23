namespace ChatClient.Services.Hubs;

public class ChatHub : Microsoft.AspNetCore.SignalR.Hub
{
    public async Task SendMessage(string user, string message)
    {
        await Clients.All.SendCoreAsync("ReceiveMessage", 
        new object[] { user, message });
    }
}
