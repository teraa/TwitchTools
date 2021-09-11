using IrcMessageParser;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Twitch.Irc;
using TwitchTools.Utils;

namespace TwitchTools.Commands
{
    public class BanToolCommand : ICommand
    {
        // Arg
        public string Channel { get; set; } = null!;
        public string Command { get; set; } = null!;
        public string Arguments { get; set; } = null!;
        // Opt
        public int Limit { get; set; }
        public int Period { get; set; }
        public bool Wait { get; set; }
        public string Login { get; set; } = null!;
        public string Token { get; set; } = null!;

        public async Task RunAsync()
        {
            if (Login is null)
                Program.Error("Login not set.");

            if (Token is null)
                Program.Error("Token not set.");

            var users = ConsoleUtils.GetInputList("Enter usernames:", @"\W+")
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();

            if (!ConsoleUtils.GetAnswer($"Running command: \"/{Command} {{user}} {Arguments}\"\non {users.Count} users, continue?", true))
            {
                Console.WriteLine("Abort.");
                return;
            }

            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.AddDebug();
                builder.AddFilter("*", LogLevel.Trace);
            });

            var logger = loggerFactory.CreateLogger<BanToolCommand>();

            using var client = new TwitchIrcClient
            (
                options: new()
                {
                    CommandLimit = new(Limit, TimeSpan.FromSeconds(Period)),
                    PingInterval = TimeSpan.FromMinutes(4),
                },
                logger: loggerFactory.CreateLogger<TwitchIrcClient>()
            );

            using var sem = new SemaphoreSlim(0);

            Task Ready()
            {
                client.Ready -= Ready;
                sem.Release();
                return Task.CompletedTask;
            }
            client.Ready += Ready;
            client.RawMessageReceived += m =>
            {
                logger.LogTrace($"recv: {m}");
                return Task.CompletedTask;
            };
            client.IrcMessageSent += m =>
            {
                if (m.Command != IrcCommand.PASS)
                    logger.LogTrace($"send: {m}");

                return Task.CompletedTask;
            };

            await client.ConnectAsync(Login!, Token!);
            await sem.WaitAsync();

            var tasks = new List<Task>();
            foreach (var user in users)
            {
                var message = new IrcMessage
                {
                    Command = IrcCommand.PRIVMSG,
                    Arg = $"#{Channel}",
                    Content = new($"/{Command} {user} {Arguments}"),
                };
                tasks.Add(client.SendAsync(message));
            }

            await Task.WhenAll(tasks);

            if (Wait && !Console.IsInputRedirected)
            {
                var exitKey = new ConsoleKeyInfo('q', ConsoleKey.Q, shift: false, alt: false, control: false);
                Console.WriteLine($"Press {exitKey.KeyChar} to quit.");
                while (Console.ReadKey(true) != exitKey) ;
            }

            await client.DisconnectAsync();
        }
    }
}
