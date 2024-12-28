using chatbot;
using Discord.WebSocket;
using OpenAI.Chat;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

Console.WriteLine("Loading variables...");
Config config = Config.LoadFromEnvFile();

// Set up dependency injection
var services = new ServiceCollection()
    .AddHttpClient()
    .AddLogging(builder =>
    {
        builder.AddSimpleConsole();
        builder.AddSeq(config.SeqUrl, config.SeqApiKey);
    }).BuildServiceProvider();

ILogger<Program> log = services.GetRequiredService<ILogger<Program>>();

var cancellationTokenSource = new CancellationTokenSource();
Console.CancelKeyPress += (sender, e) =>
{
    e.Cancel = true;
    cancellationTokenSource.Cancel();
    log.LogInformation("Cancellation requested via Ctrl+C.");
};

log.LogInformation("Constructing Discord config...");
var discordConfig = new DiscordSocketConfig { MessageCacheSize = 100, GatewayIntents = Discord.GatewayIntents.AllUnprivileged | Discord.GatewayIntents.MessageContent };
var client = new DiscordSocketClient(discordConfig);
bool isConnected = false;
var aiClient = new ChatClient(model: "gpt-4o", apiKey: config.OpenAiApiKey);
var archive = await Archive.CreateAsync();

var bot = new Bot { Client = client, Config = config, AI = aiClient, Archive = archive, Logger = log, Cancellation = cancellationTokenSource };
client.Log += LogAsync;
client.Ready += ReadyAsync;

Task LogAsync(Discord.LogMessage arg)
{
    log.LogInformation(arg.Message);
    return Task.CompletedTask;
}

Task ReadyAsync()
{
    if (isConnected)
    {
        log.LogWarning("ReadyAsync was called, but discord User is already connected!");
        return Task.CompletedTask;
    }

    isConnected = true;
    client.MessageReceived += async (arg) => await Task.Run(() => bot.MessageReceivedAsync(arg));
    client.InteractionCreated += async (arg) => await Task.Run(() => bot.InteractionCreatedAsync(arg));
    log.LogInformation($"Discord User {client.CurrentUser} is connected!");
    return Task.CompletedTask;
}

log.LogInformation("Logging into Discord...");
await client.LoginAsync(Discord.TokenType.Bot, config.DiscordToken);
log.LogInformation("Connecting to Discord...");
await client.StartAsync();
log.LogInformation("Ready. Press cancel (Ctrl+C) or send !shutdown message in discord to exit.");

try
{
    await Task.Delay(Timeout.Infinite, cancellationTokenSource.Token);
}
catch (TaskCanceledException)
{
    log.LogInformation("Cancellation requested, shutting down...");
}
catch (Exception ex)
{
    log.LogError(ex, "An error occurred. Shutting down...");
}

log.LogInformation("Logging out...");
await client.LogoutAsync();
log.LogInformation("Disposing client...");
await client.DisposeAsync();
log.LogInformation("Shutdown complete.");