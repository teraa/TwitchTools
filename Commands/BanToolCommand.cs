using IrcMessageParser;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Twitch.Irc;
using TwitchTools.Utils;

namespace TwitchTools
{
    public static class BanToolCommand
    {
        public class Args
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

        public static async Task RunAsync(Args args)
        {
            args.Login ??= Program.GetEnvironmentVariableOrError(Program.EnvLogin);
            args.Token ??= Program.GetEnvironmentVariableOrError(Program.EnvChatToken);

            var users = ConsoleUtils.GetInputList("Enter usernames:", @"\W+")
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();

            if (!ConsoleUtils.GetAnswer($"Running command: \"/{args.Command} {{user}} {args.Arguments}\"\non {users.Count} users, continue?", true))
            {
                Console.WriteLine("Abort.");
                return;
            }

            using var client = new TwitchIrcClient
            (
                options: new()
                {
                    CommandLimit = new(args.Limit, TimeSpan.FromSeconds(args.Period)),
                    PingInterval = TimeSpan.FromMinutes(4),
                },
                logger: new MyLogger<TwitchIrcClient>()
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
                ConsoleUtils.Write($"> {m}\n", ConsoleColor.DarkYellow);
                return Task.CompletedTask;
            };
            client.IrcMessageSent += m =>
            {
                if (m.Command != IrcCommand.PASS)
                    ConsoleUtils.Write($"< {m}\n", ConsoleColor.DarkGreen);

                return Task.CompletedTask;
            };

            await client.ConnectAsync(args.Login, args.Token);
            await sem.WaitAsync();

            var tasks = new List<Task>();
            foreach (var user in users)
            {
                var message = new IrcMessage
                {
                    Command = IrcCommand.PRIVMSG,
                    Arg = $"#{args.Channel}",
                    Content = new($"/{args.Command} {user} {args.Arguments}"),
                };
                tasks.Add(client.SendAsync(message));
            }

            await Task.WhenAll(tasks);

            if (args.Wait && !Console.IsInputRedirected)
            {
                var exitKey = new ConsoleKeyInfo('q', ConsoleKey.Q, shift: false, alt: false, control: false);
                Console.WriteLine($"Press {exitKey.KeyChar} to quit.");
                while (Console.ReadKey(true) != exitKey) ;
            }

            await client.DisconnectAsync();
        }
    }
    public class MyLogger<T> : ILogger<T>
    {
        public static readonly object Lock = new();

        public IDisposable BeginScope<TState>(TState state)
        {
            throw new NotImplementedException();
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            lock (Lock)
            {
                var str = formatter(state, exception);
                var color = Console.ForegroundColor;
                Console.ForegroundColor = logLevel switch
                {
                    LogLevel.Warning => ConsoleColor.Yellow,
                    LogLevel.Error => ConsoleColor.Red,
                    _ => ConsoleColor.Gray
                };
                Console.WriteLine(str);
                if (exception is not null)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine(exception);
                }
                Console.ForegroundColor = color;
            }
        }
    }
}
