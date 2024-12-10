
using chatbot;
using Discord.WebSocket;

Console.WriteLine("Loading variables...");
Config config = Config.LoadFromEnvFile();

var cancellationTokenSource = new CancellationTokenSource();
Console.CancelKeyPress += (sender, e) =>
{
    e.Cancel = true;
    cancellationTokenSource.Cancel();
};

Console.WriteLine("Constructing Discord config...");
var discordConfig = new DiscordSocketConfig { MessageCacheSize = 100, GatewayIntents = Discord.GatewayIntents.AllUnprivileged | Discord.GatewayIntents.MessageContent };
var client = new DiscordSocketClient(discordConfig);
bool isConnected = false;
var bot = new Bot { Client = client, Config = config, Cancellation = cancellationTokenSource };
client.Log += LogAsync;
client.Ready += ReadyAsync;

Task LogAsync(Discord.LogMessage arg)
{
    Console.WriteLine(arg.ToString());
    return Task.CompletedTask;
}

Task ReadyAsync()
{
    if (isConnected)
    {
        Console.WriteLine("ReadyAsync was called, but discord User is already connected!");
        return Task.CompletedTask;
    }

    isConnected = true;
    client.MessageReceived += async (arg) => await Task.Run(() => bot.MessageReceivedAsync(arg));
    client.InteractionCreated += async (arg) => await Task.Run(() => bot.InteractionCreatedAsync(arg));
    Console.WriteLine($"Discord User {client.CurrentUser} is connected!");
    return Task.CompletedTask;
}

Console.WriteLine("Logging into Discord...");
await client.LoginAsync(Discord.TokenType.Bot, config.DiscordToken);
Console.WriteLine("Connecting to Discord...");
await client.StartAsync();
Console.WriteLine("Ready. Press cancel (Ctrl+C) or send !shutdown message in discord to exit.");

try
{
    await Task.Delay(Timeout.Infinite, cancellationTokenSource.Token);
}
catch (TaskCanceledException)
{
    Console.WriteLine("Cancellation requested, shutting down...");
}
catch (Exception ex)
{
    Console.WriteLine($"An error occurred: {ex.Message}. Shutting down...");
}

Console.WriteLine("Logging out...");
await client.LogoutAsync();
Console.WriteLine("Disposing client...");
await client.DisposeAsync();
Console.WriteLine("Shutdown complete.");

