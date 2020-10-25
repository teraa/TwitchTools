using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Linq;
using TwitchTools.Utils;

namespace TwitchTools
{
    public enum Direction
    {
        Asc,
        Desc
    }

    public enum InfoSort
    {
        None,
        Date,
        Name
    }

    partial class Program
    {
        private const string TimestampFormat = "yyyy-MM-dd HH:mm:ss";
        private const string EnvToken = "TW_TOKEN";
        private const string EnvLogin = "TW_LOGIN";
        private const string EnvTokenClientId = "TW_TOKEN_CLIENT_ID"; // Client ID used to generate the token
        private const string EnvClientId = "TW_CLIENT_ID";
        private const int DefaultRequestLimit = 100;
        private const int DefaultFollowLimit = 100;
        private const Direction DefaultFollowDirection = Direction.Desc;
        private const int DefaultBantoolPeriod = 30;
        private const int DefaultBantoolLimit = 95;
        private const string DefaultBantoolCommand = "ban";

        static void Main(string[] args)
        {
            var rootCommand = new RootCommand();

            var followersCommand = new Command(
                name: "followers",
                description: "get a list of channel followers")
            {
                new Argument<string>("channel"),
                new Option<int>(
                    aliases: new[] { "-l", "--limit" },
                    getDefaultValue: () => DefaultFollowLimit,
                    description: "number of users to fetch"),
                new Option<int>(
                    aliases: new[] { "-o", "--offset" }, "starting offset"),
                new Option<Direction>(
                    aliases: new[] { "--direction" },
                    getDefaultValue: () => DefaultFollowDirection),
                new Option<string>(
                    aliases: new[] { "--cursor" },
                    description: "cursor from where to start fetching")
            };
            followersCommand.Handler = CommandHandler.Create<FollowersArguments>(Followers);
            rootCommand.AddCommand(followersCommand);

            var followingCommand = new Command(
                name: "following",
                description: "get a list of channels the user is following")
            {
                new Argument<string>("channel"),
                new Option<int>(
                    aliases: new[] { "-l", "--limit" },
                    getDefaultValue: () => DefaultFollowLimit, description: "number of users to fetch"),
                new Option<int>(
                    aliases: new[] { "-o", "--offset" },
                    description: "starting offset"),
                new Option<Direction>(
                    aliases: new[] { "--direction" },
                    getDefaultValue: () => DefaultFollowDirection)
            };
            followingCommand.Handler = CommandHandler.Create<FollowingArguments>(Following);
            rootCommand.AddCommand(followingCommand);

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

            rootCommand.Invoke(args);
        }

        static void Error(string message)
        {
            Console.Error.WriteLine($"Error: {message}\n");
            Environment.Exit(1);
        }

        static string GetEnvironmentVariableOrError(string key)
        {
            var value = Environment.GetEnvironmentVariable(key);
            if (value is null)
                Error($"Missing {key} environment variable.");
            return value;
        }
    }
}
