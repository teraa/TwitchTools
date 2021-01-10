using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;

namespace TwitchTools
{
    partial class Program
    {
        private const string TimestampFormat = "yyyy-MM-dd HH:mm:ss";
        private const string EnvToken = "TW_TOKEN";
        private const string EnvLogin = "TW_LOGIN";
        private const string EnvClientId = "TW_CLIENT_ID";
        private const int DefaultRequestLimit = 100;
        private const int DefaultFollowLimit = 100;
        private const int DefaultBantoolPeriod = 30;
        private const int DefaultBantoolLimit = 95;
        private const string DefaultBantoolCommand = "ban";

        static void Main(string[] args)
        {
            var rootCommand = new RootCommand();

            #region follows
            var followsCommand = new Command(
                name: "follows",
                description: "get a list of follows")
            {
                new Argument<FollowOrigin>("origin"),
                new Argument<string>("user"),
                new Option<bool>(
                    aliases: new[] { "-i", "--is-id" },
                    getDefaultValue: () => false,
                    description: "indicates that the provided user argument is a user ID rather than a username"
                ),
                new Option<int>(
                    aliases: new[] { "-l", "--limit" },
                    getDefaultValue: () => DefaultFollowLimit,
                    description: "number of users to fetch"),
                new Option<string>(
                    aliases: new[] { "--cursor" },
                    description: "cursor from where to start fetching")
            };
            followsCommand.Handler = CommandHandler.Create<FollowsCommandArgs>(Follows);
            rootCommand.AddCommand(followsCommand);
            #endregion

            #region info
            var infoCommand = new Command(
                name: "info",
                description: "print user info")
            {
                new Argument<IEnumerable<string>>("username"),
                new Option<InfoSort>(
                    aliases: new[] { "-s", "--sort" },
                    getDefaultValue: () => InfoSort.None,
                    description: "sort results by")
            };
            infoCommand.Handler = CommandHandler.Create<IEnumerable<string>, InfoSort>(Info);
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
                    getDefaultValue: () => DefaultBantoolCommand,
                    description: "command to execute"),
                new Argument<string>(
                    name: "arguments",
                    getDefaultValue: () => "",
                    description: "command arguments"),
                new Option<int>(
                    aliases: new[] { "-l", "--limit" },
                    getDefaultValue: () => DefaultBantoolLimit,
                    description: "maximum number of action per period"),
                new Option<int>(
                    aliases: new[] { "-p", "--period" },
                    getDefaultValue: () => DefaultBantoolPeriod,
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
            banToolCommand.Handler = CommandHandler.Create<BanToolArguments>(BanTool);
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
