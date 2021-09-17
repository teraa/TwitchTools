using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Threading;
using System.Threading.Tasks;
using TwitchTools.Commands;

namespace TwitchTools
{
    public class Program
    {
        private const string TimestampFormat = "yyyy-MM-dd HH:mm:ss";
        private const string EnvToken = "ACCESSTOKEN";
        private const string EnvClientId = "CLIENTID";
        private const string EnvLogin = "CHAT_LOGIN";
        private const string EnvChatToken = "CHAT_TOKEN";

        static async Task<int> Main(string[] args)
        {
            var rootCommand = new RootCommand();

            #region reused options
            var isIdOpt = new Option<bool>(
                aliases: new[] { "-i", "--is-id" },
                getDefaultValue: () => false,
                description: "Indicates that the provided user argument is a user ID rather than a username");

            var clientIdOpt = new Option<string>(
                aliases: new[] { "--client-id" },
                getDefaultValue: () => Environment.GetEnvironmentVariable(EnvClientId)!,
                description: $"Client ID");

            var accessTokenOpt = new Option<string>(
                aliases: new[] { "--token" },
                getDefaultValue: () => Environment.GetEnvironmentVariable(EnvToken)!,
                description: $"Access token");

            var timestampOpt = new Option<string>(
                aliases: new[] { "--timestamp" },
                getDefaultValue: () => TimestampFormat,
                description: "Timestamp format");
            #endregion


            #region follows
            var followsCommand = new Command(
                name: "follows",
                description: "Get a list of follows")
            {
                new Argument<FollowsCommand.FollowOrigin>("origin"),
                new Argument<string>("user"),
                isIdOpt,
                new Option<int?>(
                    aliases: new[] { "-l", "--limit" },
                    getDefaultValue: () => null,
                    description: "Number of users to fetch"),
                new Option<string?>(
                    aliases: new[] { "-a", "--after" },
                    description: "Cursor from where to start fetching"),
                new Option<bool>(
                    aliases: new[] { "-c", "--print-cursor" },
                    getDefaultValue: () => false,
                    description: "Print the last cursor at the end"),
                new Option<bool>(
                    aliases: new[] { "-n", "--line-numbers" },
                    getDefaultValue: () => true,
                    description: "Print line numbers"),
                clientIdOpt,
                accessTokenOpt,
                timestampOpt,
            };
            followsCommand.Handler = CommandHandler.Create((FollowsCommand c, CancellationToken t) => c.RunAsync(t));
            rootCommand.AddCommand(followsCommand);
            #endregion


            #region info
            var infoCommand = new Command(
                name: "info",
                description: "Get info of a user")
            {
                new Argument<string>("user"),
                isIdOpt,
                clientIdOpt,
                accessTokenOpt,
                timestampOpt,
            };
            infoCommand.Handler = CommandHandler.Create((InfoCommand c, CancellationToken t) => c.RunAsync(t));
            rootCommand.AddCommand(infoCommand);
            #endregion


            #region infobatch
            var infoBatchCommand = new Command(
                name: "infobatch",
                description: "Get info of multiple users")
            {
                new Argument<IEnumerable<string>?>("users"),
                isIdOpt,
                new Option<InfoBatchCommand.InfoSort?>(
                    aliases: new[] { "-s", "--sort-by" },
                    getDefaultValue: () => InfoBatchCommand.InfoSort.None,
                    description: "Sort results by"),
                clientIdOpt,
                accessTokenOpt,
                timestampOpt,
            };
            infoBatchCommand.Handler = CommandHandler.Create((InfoBatchCommand c, CancellationToken t) => c.RunAsync(t));
            rootCommand.AddCommand(infoBatchCommand);
            #endregion


            #region bantool
            var banToolCommand = new Command(
                name: "bantool",
                description: "Execute commands in a channel for each specified user")
            {
                new Argument<string>(
                    name: "channel",
                    description: "Channel to execute the commands in"),
                new Argument<string>(
                    name: "command",
                    getDefaultValue: () => "ban",
                    description: "Command to execute"),
                new Argument<string>(
                    name: "arguments",
                    getDefaultValue: () => "",
                    description: "Command arguments"),
                new Option<int>(
                    aliases: new[] { "-l", "--limit" },
                    getDefaultValue: () => 95,
                    description: "Maximum number of action per period"),
                new Option<int>(
                    aliases: new[] { "-p", "--period" },
                    getDefaultValue: () => 30,
                    description: "Period (in seconds) in which a limited number of actions can be performed"),
                new Option<bool>(
                    aliases: new[] { "-w", "--wait" },
                    getDefaultValue: () => true,
                    description: "Wait for keypress to terminate the program after executing all the commands"),
                new Option<string>(
                    aliases: new[] { "--login" },
                    getDefaultValue: () => Environment.GetEnvironmentVariable(EnvLogin)!,
                    description: $"Login username"),
                new Option<string>(
                    aliases: new[] { "--token" },
                    getDefaultValue: () => Environment.GetEnvironmentVariable(EnvChatToken)!,
                    description: $"OAuth token")
            };
            banToolCommand.Handler = CommandHandler.Create((BanToolCommand c, CancellationToken t) => c.RunAsync(t));
            rootCommand.AddCommand(banToolCommand);
            #endregion


            return await rootCommand.InvokeAsync(args);
        }
    }
}
