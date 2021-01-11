using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using TwitchTools.Commands;

namespace TwitchTools
{
    public class Program
    {
        internal const string TimestampFormat = "yyyy-MM-dd HH:mm:ss";
        internal const string EnvToken = "ACCESSTOKEN";
        internal const string EnvLogin = "LOGIN";
        internal const string EnvClientId = "CLIENTID";

        static void Main(string[] args)
        {
            var rootCommand = new RootCommand();

            #region follows
            var followsCommand = new Command(
                name: "follows",
                description: "get a list of follows")
            {
                new Argument<FollowsCommand.FollowOrigin>("origin"),
                new Argument<string>("user"),
                new Option<bool>(
                    aliases: new[] { "-i", "--is-id" },
                    getDefaultValue: () => false,
                    description: "indicates that the provided user argument is a user ID rather than a username"
                ),
                new Option<int>(
                    aliases: new[] { "-l", "--limit" },
                    getDefaultValue: () => 100,
                    description: "number of users to fetch"),
                new Option<string>(
                    aliases: new[] { "--cursor" },
                    description: "cursor from where to start fetching")
            };
            followsCommand.Handler = CommandHandler.Create<FollowsCommand.Args>(FollowsCommand.RunAsync);
            rootCommand.AddCommand(followsCommand);
            #endregion

            #region info
            var infoCommand = new Command(
                name: "info",
                description: "print user info")
            {
                new Argument<IEnumerable<string>>("username"),
                new Option<InfoCommand.InfoSort>(
                    aliases: new[] { "-s", "--sort" },
                    getDefaultValue: () => InfoCommand.InfoSort.None,
                    description: "sort results by")
            };
            infoCommand.Handler = CommandHandler.Create<IEnumerable<string>, InfoCommand.InfoSort>(InfoCommand.RunAsync);
            rootCommand.AddCommand(infoCommand);
            #endregion

            #region bantool
            var banToolCommand = new Command(
                name: "bantool",
                description: "execute commands in a channel for each specified user")
            {
                new Argument<string>(
                    name: "channel",
                    description: "channel to execute the commands in"),
                new Argument<string>(
                    name: "command",
                    getDefaultValue: () => "ban",
                    description: "command to execute"),
                new Argument<string>(
                    name: "arguments",
                    getDefaultValue: () => "",
                    description: "command arguments"),
                new Option<int>(
                    aliases: new[] { "-l", "--limit" },
                    getDefaultValue: () => 95,
                    description: "maximum number of action per period"),
                new Option<int>(
                    aliases: new[] { "-p", "--period" },
                    getDefaultValue: () => 30,
                    description: "period (in seconds) in which a limited number of actions can be performed"),
                new Option<bool>(
                    aliases: new[] { "-w", "--wait" },
                    getDefaultValue: () => true,
                    description: "wait for keypress to terminate the program after executing all the commands"),
                new Option<string>(
                    aliases: new[] { "--login" },
                    description: $"login username"),
                new Option<string>(
                    aliases: new[] { "--token" },
                    description: $"OAuth token")
            };
            banToolCommand.Handler = CommandHandler.Create<BanToolCommand.Args>(BanToolCommand.RunAsync);
            rootCommand.AddCommand(banToolCommand);
            #endregion

            rootCommand.Invoke(args);
        }

        public static void Error(string message)
        {
            Console.Error.WriteLine($"Error: {message}\n");
            Environment.Exit(1);
        }

        public static string GetEnvironmentVariableOrError(string key)
        {
            var value = Environment.GetEnvironmentVariable(key);
            if (value is null)
                Error($"Missing {key} environment variable.");
            return value;
        }
    }
}
