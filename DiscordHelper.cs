using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace chatbot
{
    internal class DiscordHelper
    {
        internal static string ReplaceUserTagsWithNicknames(IMessage msg)
        {
            var text = new StringBuilder(msg.Content);
            var tags = msg.Tags;
            int indexOffset = 0;
            foreach (var tag in tags)
            {
                if (tag.Type != TagType.UserMention)
                    continue;

                var user = tag.Value as IUser;
                var guildUser = user as IGuildUser;
                string? nick = guildUser?.Nickname ?? user?.GlobalName ?? user?.Username;
                if (!string.IsNullOrEmpty(nick))
                {
                    text.Remove(tag.Index + indexOffset, tag.Length);
                    text.Insert(tag.Index + indexOffset, nick);
                    indexOffset += nick.Length - tag.Length;
                }
            }

            return text.ToString();
        }
    }
}
