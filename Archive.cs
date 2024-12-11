using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace chatbot
{
    internal class Archive : IDisposable
    {
        public required SqliteConnection Connection { get; internal init; }
        private Archive()
        {
        }
        
        public static async Task<Archive> CreateAsync()
        {
            SqliteConnectionStringBuilder conStringBuilder = new();
            conStringBuilder.DataSource = ":memory:";
            SqliteConnection connection = new(conStringBuilder.ConnectionString);
            await connection.OpenAsync();
            using var command = connection.CreateCommand();
            command.CommandText = "CREATE TABLE Messages (Id INTEGER PRIMARY KEY, Content TEXT NOT NULL, Type INTEGER NOT NULL, ChannelId INTEGER NOT NULL)";
            await command.ExecuteNonQueryAsync();

            return new Archive { Connection = connection };
        }

        public void Dispose()
        {
            Connection.Dispose();
        }

        public Task AddMessageAsync(ulong id, string content, ArchivedMessageType type, ulong channelId)
        {
            using var command = Connection.CreateCommand();
            command.CommandText = "INSERT OR IGNORE INTO Messages(Id, Content, Type, ChannelId) VALUES(@Id, @Content, @Type, @ChannelId)";
            command.Parameters.AddWithValue("@Id", id);
            command.Parameters.AddWithValue("@Content", content);
            command.Parameters.AddWithValue("@Type", type);
            command.Parameters.AddWithValue("@ChannelId", channelId);
            return command.ExecuteNonQueryAsync();
        }

        public async Task<List<ArchivedMessage>> GetLastMessagesForChannelAsync(ulong channelId, int count)
        {
            using var command = Connection.CreateCommand();
            command.CommandText = "SELECT * FROM (SELECT * FROM Messages WHERE ChannelId = @ChannelId ORDER BY Id DESC LIMIT @Count) ORDER BY Id ASC";
            command.Parameters.AddWithValue("@ChannelId", channelId);
            command.Parameters.AddWithValue("@Count", count);
            using var reader = await command.ExecuteReaderAsync();
            List<ArchivedMessage> messages = new();
            while (await reader.ReadAsync())
            {
                messages.Add(new ArchivedMessage()
                {
                    Id = await reader.GetFieldValueAsync<ulong>(0),
                    Content = await reader.GetFieldValueAsync<string>(1),
                    Type = await reader.GetFieldValueAsync<ArchivedMessageType>(2),
                    ChannelId = await reader.GetFieldValueAsync<ulong>(3)
                });
            }
            return messages;
        }
    }

    public class ArchivedMessage
    {
        public ulong Id { get; set; }
        public required string Content { get; set; }
        public ArchivedMessageType Type { get; set; }
        public ulong ChannelId { get; set; }
    }

    public enum ArchivedMessageType
    {
        UserMessage,
        BotMessage,
    }
}