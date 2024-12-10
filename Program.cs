
using Discord.WebSocket;

DotNetEnv.Env.TraversePath().Load();
var discordToken = Environment.GetEnvironmentVariable("DISCORD_TOKEN");
if(string.IsNullOrEmpty(discordToken))
{
    Console.WriteLine("DISCORD_TOKEN is not set.");
    return;
}


var config = new DiscordSocketConfig { MessageCacheSize = 100, GatewayIntents = Discord.GatewayIntents.AllUnprivileged | Discord.GatewayIntents.MessageContent };
var client = new DiscordSocketClient(config);
client.Log += LogAsync;
client.Ready += ReadyAsync;
client.MessageReceived += MessageReceivedAsync;
client.InteractionCreated += InteractionCreatedAsync;
bool isReady = false;

Task LogAsync(Discord.LogMessage arg)
{
    Console.WriteLine(arg.ToString());
    return Task.CompletedTask;
}

Task ReadyAsync()
{
    isReady = true;
    Console.WriteLine($"{client.CurrentUser} is connected!");
    return Task.CompletedTask;
}

Task MessageReceivedAsync(SocketMessage arg)
{
    throw new NotImplementedException();
}

Task InteractionCreatedAsync(SocketInteraction interaction)
{
    throw new NotImplementedException();
}

Console.WriteLine("Hello, World!");
await client.LoginAsync(Discord.TokenType.Bot, discordToken);
await client.StartAsync();
await Task.Delay(Timeout.Infinite);


