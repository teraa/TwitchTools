using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Threading.Tasks;
using TwitchTools.Commands;

namespace TwitchTools
{
    public class Program
    {
        internal const string TimestampFormat = "yyyy-MM-dd HH:mm:ss";
        internal const string EnvToken = "ACCESSTOKEN";
        internal const string EnvClientId = "CLIENTID";
        internal const string EnvLogin = "CHAT_LOGIN";
        internal const string EnvChatToken = "CHAT_TOKEN";

        static async Task Main(string[] args)
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
                    description: "indicates that the provided user argument is a user ID rather than a username"),
                new Option<int>(
                    aliases: new[] { "-l", "--limit" },
                    getDefaultValue: () => 100,
                    description: "number of users to fetch"),
                new Option<string?>(
                    aliases: new[] { "-a", "--after" },
                    description: "cursor from where to start fetching"),
                new Option<bool>(
                    aliases: new[] { "-c", "--print-cursor" },
                    getDefaultValue: () => false,
                    description: "print the last cursor at the end"),
                new Option<string>(
                    aliases: new[] { "--client-id" },
                    getDefaultValue: () => Environment.GetEnvironmentVariable(EnvClientId)!,
                    description: $"Client ID"),
                new Option<string>(
                    aliases: new[] { "--token" },
                    getDefaultValue: () => Environment.GetEnvironmentVariable(EnvToken)!,
                    description: $"Access token")
            };
            followsCommand.Handler = CommandHandler.Create<FollowsCommand>(x => x.RunAsync());
            rootCommand.AddCommand(followsCommand);
            #endregion

            #region info
            var infoCommand = new Command(
                name: "info",
                description: "get info of a user")
            {
                new Argument<string>("user"),
                new Option<bool>(
                    aliases: new[] { "-i", "--is-id" },
                    getDefaultValue: () => false,
                    description: "indicates that the provided user argument is a user ID rather than a username"),
                new Option<string>(
                    aliases: new[] { "--client-id" },
                    getDefaultValue: () => Environment.GetEnvironmentVariable(EnvClientId)!,
                    description: $"Client ID"),
                new Option<string>(
                    aliases: new[] { "--token" },
                    getDefaultValue: () => Environment.GetEnvironmentVariable(EnvToken)!,
                    description: $"Access token")
            };
            infoCommand.Handler = CommandHandler.Create<InfoCommand>(x => x.RunAsync());
            rootCommand.AddCommand(infoCommand);
            #endregion

            #region infobatch
            var infoBatchCommand = new Command(
                name: "infobatch",
                description: "get info of multiple users")
            {
                new Argument<IEnumerable<string>?>("users"),
                new Option<bool>(
                    aliases: new[] { "-i", "--is-id" },
                    getDefaultValue: () => false,
                    description: "indicates that the provided user argument is a user ID rather than a username"),
                new Option<InfoBatchCommand.InfoSort?>(
                    aliases: new[] { "-s", "--sort-by" },
                    getDefaultValue: () => InfoBatchCommand.InfoSort.None,
                    description: "sort results by"),
                new Option<string>(
                    aliases: new[] { "--client-id" },
                    getDefaultValue: () => Environment.GetEnvironmentVariable(EnvClientId)!,
                    description: $"Client ID"),
                new Option<string>(
                    aliases: new[] { "--token" },
                    getDefaultValue: () => Environment.GetEnvironmentVariable(EnvToken)!,
                    description: $"Access token")
            };
            infoBatchCommand.Handler = CommandHandler.Create<InfoBatchCommand>(x => x.RunAsync());
            rootCommand.AddCommand(infoBatchCommand);
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
                    getDefaultValue: () => Environment.GetEnvironmentVariable(EnvLogin)!,
                    description: $"login username"),
                new Option<string>(
                    aliases: new[] { "--token" },
                    getDefaultValue: () => Environment.GetEnvironmentVariable(EnvChatToken)!,
                    description: $"OAuth token")
            };
            banToolCommand.Handler = CommandHandler.Create<BanToolCommand>(x => x.RunAsync());
            rootCommand.AddCommand(banToolCommand);
            #endregion

            await rootCommand.InvokeAsync(args);
        }

        public static void Error(string message)
        {
            Console.Error.WriteLine($"Error: {message}\n");
            Environment.Exit(1);
        }
    }
}
