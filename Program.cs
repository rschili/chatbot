
using Discord;
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

const string buttonId = "rk2k9f920023";
async Task MessageReceivedAsync(SocketMessage arg)
{
    // The bot should never respond to itself.
    if (arg.Author.Id == client.CurrentUser.Id)
        return;


    if (arg.Content == "!ping")
    {
        // Create a new ComponentBuilder, in which dropdowns & buttons can be created.
        var cb = new ComponentBuilder()
            .WithButton("Click me!", buttonId, ButtonStyle.Primary);

        // Send a message with content 'pong', including a button.
        // This button needs to be build by calling .Build() before being passed into the call.
        await arg.Channel.SendMessageAsync("pong!", components: cb.Build());
    }
}

async Task InteractionCreatedAsync(SocketInteraction interaction)
{
    if (interaction is SocketMessageComponent component)
    {
        // Check for the ID created in the button mentioned above.
        if (component.Data.CustomId == buttonId)
            await interaction.RespondAsync("Thank you for clicking my button!");

        else
            Console.WriteLine("An ID has been received that has no handler!");
    }
}

Console.WriteLine("Hello, World!");
await client.LoginAsync(Discord.TokenType.Bot, discordToken);
await client.StartAsync();
await Task.Delay(Timeout.Infinite);


