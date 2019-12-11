using System;
using System.Linq;
using System.Threading.Tasks;
using Twitch.Irc;
using Twitch.Utils;
using TwitchTools.Utils;

namespace TwitchTools
{
    partial class Program
    {
        static async Task BanTool(string login, string token, string channelname, string command, string commandArgs, int limit, int period)
        {
            var users = ConsoleUtils.GetInputList("Enter usernames:", @"\W+")
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();

            if (!ConsoleUtils.GetAnswer($"Running command: \"/{command} {{user}} {commandArgs}\"\non {users.Count} users, continue?", true))
            {
                Console.WriteLine("Abort.");
                return;
            }

            using var rateLimiter = new RateLimiter(TimeSpan.FromSeconds(period), limit);
            using var client = new TwitchIrcClient(login, token, new TwitchIrcConfig { PingInterval = TimeSpan.FromMinutes(4) });
            client.Log += (s, e) => ConsoleUtils.Write($"IRC: {e.Message}\n", ConsoleColor.DarkCyan);
            client.RawMessageReceived += (s, e) => ConsoleUtils.Write($"> {e.Message}\n", ConsoleColor.DarkYellow);
            client.RawMessageSent += (s, e) => ConsoleUtils.Write($"< {e.Message}\n", ConsoleColor.DarkGreen);
            await client.ConnectAsync();
            foreach (var user in users)
            {
                await rateLimiter.PerformAsync<Task>(() => client.SendCommandAsync(channelname, $"/{command} {user} {commandArgs}"));
            }
            await client.DisconnectAsync();
        }
    }
}
