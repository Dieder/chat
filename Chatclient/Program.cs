using Azure.AI.Projects;
using Azure.Identity;
using chat.Components;
using ChatClient.Services;
using ChatClient.Services.Hubs;
using Microsoft.AspNetCore.ResponseCompression;
using MudBlazor.Services;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSignalR();
builder.Services.AddMudServices();
builder.Services.AddScoped<IChatService, FoundryChatService>();
builder.Services.AddScoped<IWeatherService, WeatherService>();
builder.Services.AddLogging(logging =>
{
    logging.ClearProviders();
    logging.SetMinimumLevel(LogLevel.Information);
    logging.AddDebug();    
    logging.AddConsole();
});
builder.Services.AddSingleton(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var endpoint = config["AzureAI:ProjectEndpoint"]
        ?? throw new InvalidOperationException("AzureAI:ProjectEndpoint is not configured.");

    return new AIProjectClient(
        new Uri(endpoint),
        new DefaultAzureCredential());
});
builder.Services.AddScoped<IWeatherService, WeatherService>();
builder.Services.AddResponseCompression(opts =>
{
   opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
       [ "application/octet-stream" ]);
});
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.MapHub<ChatHub>("/chathub");
//app.MapFallbackToPage("/_Host"); //
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
app.UseResponseCompression();
app.Run();


