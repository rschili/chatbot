using Discord.WebSocket;
using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenAI.Chat;

namespace chatbot
{
    internal class Bot
    {
        public required DiscordSocketClient Client { get; init; }
        public required Config Config { get; init; }
        public required CancellationTokenSource Cancellation { get; init; }
        public Archive Archive { get; private init; }
        public ChatClient AI => _ai.Value;
        private readonly Lazy<ChatClient> _ai;

        public Bot()
        {
            Archive = Archive.Create();
            _ai = new Lazy<ChatClient>(
                () => {
                    if(Config == null)
                        throw new InvalidOperationException("Config is not set");

                    return new ChatClient(model: "gpt-4o", apiKey: Config.OpenAiApiKey);
                    }
                );
        }

        const string buttonId = "rk2k9f920023";
        public async Task MessageReceivedAsync(SocketMessage arg)
        {
            // The bot should never respond to itself.
            if (arg.Author.Id == Client.CurrentUser.Id)
                return;

            if (arg.Content == "!ping")
            {
                var button = new ComponentBuilder().WithButton("Klick!", buttonId, ButtonStyle.Primary).Build();
                await arg.Channel.SendMessageAsync("pong!", components: button);
                return;
            }
            else if (arg.Content == "!shutdown")
            {
                if (arg.Author.Id == Config.DiscordAdminId)
                {
                    await arg.Channel.SendMessageAsync("In Ordnung...", messageReference: new MessageReference(messageId: arg.Id));
                    Cancellation.Cancel();
                }
                else
                {
                    await arg.Channel.SendMessageAsync("Du bist nicht mein Chef.", messageReference: new MessageReference(messageId: arg.Id));
                }
                return;
            }

            var options = new ChatCompletionOptions
            {
                MaxOutputTokenCount = 100,
            };
            var messages = new List<ChatMessage>
            {
                ChatMessage.CreateSystemMessage("Du bist ein Discord Chatbot. Antworte so kurz wie möglich."),
                ChatMessage.CreateUserMessage(arg.Content)
            };

            try
            {
                var response = await AI.CompleteChatAsync(messages, options);
                if (response.Value.FinishReason != ChatFinishReason.Stop)
                {
                    Console.WriteLine($"OpenAI call did not finish with Stop. Value was {response.Value.FinishReason}");
                    return;
                }
                foreach(var content in response.Value.Content)
                {
                    if(content.Kind != ChatMessageContentPartKind.Text || !string.IsNullOrEmpty(content.Text))
                        await arg.Channel.SendMessageAsync(content.Text);
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Ein Fehler ist aufgetreten beim call von OpenAI: {ex.Message}");
            }
        }

        public async Task InteractionCreatedAsync(SocketInteraction interaction)
        {
            if (interaction is SocketMessageComponent component)
            {
                // Check for the ID created in the button mentioned above.
                if (component.Data.CustomId == buttonId)
                    await interaction.RespondAsync("Danke, dass du meinen Knopf gedrückt hast.");
                else
                    Console.WriteLine("An ID has been received that has no handler!");
            }
        }
    }
}
