using System;
using System.Linq;
using System.Threading.Tasks;
using Twitch.Irc;
using Twitch.Utils;
using TwitchTools.Utils;

namespace TwitchTools
{
    public class BanToolArguments
    {
        public string Channel { get; set; }
        public string Command { get; set; }
        public string Arguments { get; set; }
        public int Limit { get; set; }
        public int Period { get; set; }
        public bool Wait { get; set; }
        public string Login { get; set; }
        public string Token { get; set; }
    }

    partial class Program
    {
        static async Task BanTool(BanToolArguments args)
        {
            args.Login ??= GetEnvironmentVariableOrError(EnvLogin);
            args.Token ??= GetEnvironmentVariableOrError(EnvToken);

            var users = ConsoleUtils.GetInputList("Enter usernames:", @"\W+")
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();

            if (!ConsoleUtils.GetAnswer($"Running command: \"/{args.Command} {{user}} {args.Arguments}\"\non {users.Count} users, continue?", true))
            {
                Console.WriteLine("Abort.");
                return;
            }

            using var rateLimiter = new RateLimiter(TimeSpan.FromSeconds(args.Period), args.Limit);
            using var client = new TwitchIrcClient(args.Login, args.Token, new TwitchIrcConfig { PingInterval = TimeSpan.FromMinutes(4) });
            client.Log += (s, e) => ConsoleUtils.Write($"IRC: {e.Message}\n", ConsoleColor.DarkCyan);
            client.RawMessageReceived += (s, e) => ConsoleUtils.Write($"> {e.Message}\n", ConsoleColor.DarkYellow);
            client.RawMessageSent += (s, e) => ConsoleUtils.Write($"< {e.Message}\n", ConsoleColor.DarkGreen);
            await client.ConnectAsync();
            foreach (var user in users)
            {
                await rateLimiter.PerformAsync<Task>(() => client.SendCommandAsync(args.Channel, $"/{args.Command} {user} {args.Arguments}"));
            }

            if (args.Wait && !Console.IsInputRedirected)
            {
                var exitKey = new ConsoleKeyInfo('q', ConsoleKey.Q, shift: false, alt: false, control: false);
                Console.WriteLine($"Press {exitKey.KeyChar} to quit.");
                while (Console.ReadKey(true) != exitKey) ;
            }

            await client.DisconnectAsync();
        }
    }
}
