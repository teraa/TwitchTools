using IrcMessageParser;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Twitch;
using Twitch.Irc;
using static TwitchTools.ConsoleUtils;

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
        public string? Login { get; set; }
        public string? Token { get; set; }

        public async Task<int> RunAsync()
        {
            if (Login is null)
            {
                Error("Login not set.");
                return 1;
            }

            if (Token is null)
            {
                Error("Token not set.");
                return 1;
            }

            var users = GetInputList("Enter usernames:", @"\W+")
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.AddDebug();
                builder.AddFilter("*", LogLevel.Trace);
            });
            serviceCollection.AddTwitchIrcClient(options =>
            {
                options.CommandLimiter = new SlidingWindowRateLimiter(Limit, TimeSpan.FromSeconds(Period));
                options.PingInterval = TimeSpan.FromMinutes(4);
            });

            using var serviceProvider = serviceCollection.BuildServiceProvider();
            var logger = serviceProvider.GetRequiredService<ILogger<BanToolCommand>>();
            var client = serviceProvider.GetRequiredService<TwitchIrcClient>();

            if (!GetAnswer($"Running command: \"/{Command} {{user}} {Arguments}\"\non {users.Count} users, continue?", true))
            {
                logger.LogInformation("Abort.");
                return 0;
            }

            client.IrcMessageReceived += msg =>
            {
                logger.LogTrace($"recv: {msg}");
                return ValueTask.CompletedTask;
            };

            client.IrcMessageSent += msg =>
            {
                if (msg.Command != IrcCommand.PASS)
                    logger.LogTrace($"send: {msg}");

                return ValueTask.CompletedTask;
            };

            await client.ConnectAsync();
            await client.LoginAsync(Login, Token);

            var tasks = new List<Task>();
            foreach (var user in users)
            {
                var message = new IrcMessage
                {
                    Command = IrcCommand.PRIVMSG,
                    Arg = $"#{Channel}",
                    Content = new($"/{Command} {user} {Arguments}"),
                };
                tasks.Add(client.SendAsync(message).AsTask());
            }

            await Task.WhenAll(tasks);

            if (Wait && !Console.IsInputRedirected)
            {
                var exitKey = new ConsoleKeyInfo('q', ConsoleKey.Q, shift: false, alt: false, control: false);
                logger.LogInformation($"Press {exitKey.KeyChar} to quit.");
                while (Console.ReadKey(true) != exitKey) ;
            }

            await client.DisconnectAsync();

            return 0;
        }
    }
}
