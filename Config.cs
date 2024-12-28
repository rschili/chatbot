using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace chatbot
{
    internal class Config
    {
        public required string DiscordToken { get; init; }
        public required ulong DiscordAdminId { get; init; }
        public required string OpenAiApiKey { get; init; }

        public required string SeqApiKey { get; init; }
        public required string SeqUrl { get; init; }


        public static Config LoadFromEnvFile()
        {
            DotNetEnv.Env.TraversePath().Load();
            var discordToken = Environment.GetEnvironmentVariable("DISCORD_TOKEN");
            if(string.IsNullOrEmpty(discordToken))
            {
                throw new KeyNotFoundException("DISCORD_TOKEN is not set");
            }
            var discordAdminIdStr = Environment.GetEnvironmentVariable("DISCORD_ADMIN_ID");
            if (string.IsNullOrEmpty(discordAdminIdStr))
            {
                throw new KeyNotFoundException("DISCORD_ADMIN_ID is not set");
            }
            if (!ulong.TryParse(discordAdminIdStr, out ulong discordAdminId))
            {
                throw new FormatException("DISCORD_ADMIN_ID is not a valid ulong");
            }
            var openAiApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            if (string.IsNullOrEmpty(openAiApiKey))
            {
                throw new KeyNotFoundException("OPENAI_API_KEY is not set");
            }
            var seqApiKey = Environment.GetEnvironmentVariable("SEQ_API_KEY");
            if (string.IsNullOrEmpty(seqApiKey))
            {
                throw new KeyNotFoundException("SEQ_API_KEY is not set");
            }
            var seqUrl = Environment.GetEnvironmentVariable("SEQ_URL");
            if (string.IsNullOrEmpty(seqUrl))
            {
                throw new KeyNotFoundException("SEQ_URL is not set");
            }

            return new Config 
            {
                DiscordAdminId = discordAdminId,
                DiscordToken = discordToken,
                OpenAiApiKey = openAiApiKey,
                SeqApiKey = seqApiKey,
                SeqUrl = seqUrl
            };
        }
    }
}
