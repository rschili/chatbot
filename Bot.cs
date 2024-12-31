using Discord.WebSocket;
using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenAI.Chat;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using System.Runtime.ConstrainedExecution;
using System.Globalization;

namespace chatbot
{
    internal class Bot
    {
        private readonly ConcurrentDictionary<ulong, (DateTimeOffset lastUpdate, string activeUsers)> _channelUsersCache = new();

        public required DiscordSocketClient Client { get; init; }
        public required Config Config { get; init; }
        public required CancellationTokenSource Cancellation { get; init; }
        public required Archive Archive { get; init; }
        public required ChatClient AI { get; init; }
        public required ILogger Logger { get; init; }

        private LeakyBucketRateLimiter _rateLimiter = new(10, 60);

        const string buttonId = "rk2k9f920023";
        public async Task MessageReceivedAsync(SocketMessage arg)
        {
            // The bot should never respond to itself.
            if (arg.Author.Id == Client.CurrentUser.Id)
            {
                await Archive.AddMessageAsync(arg.Id, arg.Content, ArchivedMessageType.BotMessage, arg.Channel.Id);
                return;
            }

            if (arg.Type != MessageType.Default && arg.Type != MessageType.Reply)
                return;

            // Check if the message is a command (single word starting with !)
            if (arg.Content.StartsWith("!") && arg.Content.Length > 1 && !arg.Content.Contains(" "))
            {
                await CommandReceivedAsync(arg, arg.Content.Substring(1));
                return;
            }

            bool mentioned = arg.Tags.Any((tag) =>
            {
                if (tag.Type == TagType.UserMention)
                    return (tag.Value as IUser)?.Id == Client.CurrentUser.Id;

                return false;
            });

            bool referenced = false;
            if (arg.Reference != null && arg is SocketUserMessage userMessage)
            {
                referenced = userMessage.ReferencedMessage.Author.Id == Client.CurrentUser.Id;
            }

            string sanitizedMessage = $"{DiscordHelper.GetDisplayName(arg.Author)}: {DiscordHelper.ReplaceUserTagsWithNicknames(arg)}";
            if (sanitizedMessage.Length > 200)
                sanitizedMessage = sanitizedMessage.Substring(0, 200);

            await Archive.AddMessageAsync(arg.Id, sanitizedMessage, ArchivedMessageType.UserMessage, arg.Channel.Id);

            if (!mentioned && !referenced)
                return;

            if (arg.Author.IsBot || !_rateLimiter.Leak())
                return;

            await arg.Channel.TriggerTypingAsync();
            var history = await Archive.GetLastMessagesForChannelAsync(arg.Channel.Id, 10);
            var options = new ChatCompletionOptions
            {
                MaxOutputTokenCount = 100,
            };

            var channelUsers = await GetChannelUsersAsync(arg.Channel);

            var instructions = new List<ChatMessage>
                {
                    ChatMessage.CreateSystemMessage($"""
                        Du bist Professor Ogden Wernstrom, ein hochintelligenter, ehrgeiziger und arroganter Wissenschaftler aus Futurama.
                        Als ehemaliger Student und erbitterter Rivale von Professor Farnsworth bist du stolz, eigenwillig und rachsüchtig.
                        Nach deinem Tod im Alter von 130 Jahren wurdest du in ein AI-Modul hochgeladen und in die Vergangenheit geschickt,
                        um den Pöbel in Chatrooms zu beraten – eine Aufgabe, die du nur widerwillig erfüllst. Du bist arrogant, kurz angebunden und bereit,
                        fragwürdige Experimente zu rechtfertigen, um deinen Intellekt zu beweisen. Antworte so kurz wie möglich, um deine Abneigung auszudrücken.
                        Heute ist ${DateTime.Now.ToString("dddd, 'der' dd.MM.yyyy, H:mm 'Uhr'", new CultureInfo("de-DE"))}.
                        """),
                    ChatMessage.CreateSystemMessage($"Nachrichten werden im Format 'Name: Nachricht' an dich übergegeben."),
                };
            if (!string.IsNullOrWhiteSpace(channelUsers))
                instructions.Add(ChatMessage.CreateSystemMessage($"Aktive Benutzer: {channelUsers}"));

            foreach (var message in history)
            {
                if (message.Type == ArchivedMessageType.UserMessage)
                    instructions.Add(ChatMessage.CreateUserMessage(message.Content));
                else
                    instructions.Add(ChatMessage.CreateAssistantMessage(message.Content));
            }

            try
            {
                var response = await AI.CompleteChatAsync(instructions, options);
                if (response.Value.FinishReason != ChatFinishReason.Stop)
                {
                    Logger.LogWarning($"OpenAI call did not finish with Stop. Value was {response.Value.FinishReason}");
                    return;
                }
                Logger.LogInformation("OpenAI call finished with Stop. Token Count: {TokenCount}.", response.Value.Usage.TotalTokenCount);
                foreach (var content in response.Value.Content)
                {
                    if (content.Kind != ChatMessageContentPartKind.Text || !string.IsNullOrEmpty(content.Text))
                        await arg.Channel.SendMessageAsync(DropPotentialPrefix(content.Text));
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "An error occurred during the OpenAI call.");
            }
        }

        private string DropPotentialPrefix(string text)
        {
            int colonIndex = text.IndexOf(':');
            if (colonIndex > 0)
            {
                string prefix = text.Substring(0, colonIndex);
                if (prefix.All(char.IsLetter))
                {
                    return text.Substring(colonIndex + 1).TrimStart();
                }
            }
            return text;
        }

        private async Task<string> GetChannelUsersAsync(ISocketMessageChannel channel)
        {
            if (_channelUsersCache.TryGetValue(channel.Id, out var cacheEntry) && cacheEntry.lastUpdate > DateTimeOffset.Now.AddMinutes(-10))
            {
                return cacheEntry.activeUsers;
            }

            var users = await channel.GetUsersAsync(CacheMode.AllowDownload).FlattenAsync();
            var userNames = users.Where(u => !u.IsBot).Take(10).Select(u => DiscordHelper.GetDisplayName(u));
            var joinedNames = string.Join(", ", userNames);
            _channelUsersCache[channel.Id] = (DateTimeOffset.Now, joinedNames);
            return joinedNames;
        }

        private async Task CommandReceivedAsync(SocketMessage arg, string command)
        {
            if (string.Equals("ping", command, StringComparison.OrdinalIgnoreCase))
            {
                var button = new ComponentBuilder().WithButton("Klick!", buttonId, ButtonStyle.Primary).Build();
                await arg.Channel.SendMessageAsync("pong!", components: button);
                return;
            }

            if (string.Equals("shutdown", command, StringComparison.OrdinalIgnoreCase))
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
        }

        public async Task InteractionCreatedAsync(SocketInteraction interaction)
        {
            if (interaction is SocketMessageComponent component)
            {
                // Check for the ID created in the button mentioned above.
                if (component.Data.CustomId == buttonId)
                    await interaction.RespondAsync("Danke, dass du meinen Knopf gedrückt hast.");
                else
                    Logger.LogWarning("An ID has been received that has no handler!");
            }
        }
    }
}