using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Twitch.API.Helix;
using Twitch.API.Helix.Rest;

namespace TwitchTools
{
    partial class Program
    {
        private const string TimestampFormat = "yyyy-MM-dd HH:mm:ss";
        private const string EnvToken = "tw_token";
        private const string EnvLogin = "tw_login";
        private const string EnvClientId = "tw_client_id";
        private const int DefaultFollowLimit = 100;
        private const string DefaultFollowDirection = "desc";
        private const int DefaultBantoolPeriod = 30;
        private const int DefaultBantoolLimit = 95;
        private const string DefaultBantoolCommand = "ban";

        static void Main(string[] args)
            => MainAsync(args).GetAwaiter().GetResult();
        static async Task MainAsync(string[] args)
        {
            if (args.Any())
            {
                switch (args[0])
                {
                    case nameof(Followers):
                        {
                            ParseFollowArgs(args.Skip(1), out var clientId, out var userId, out var limit, out var offset, out var direction);
                            await Followers(clientId, userId, limit, offset, direction);
                        }
                        break;

                    case nameof(Following):
                        {
                            ParseFollowArgs(args.Skip(1), out var clientId, out var userId, out var limit, out var offset, out var direction);
                            await Following(clientId, userId, limit, offset, direction);
                        }
                        break;

                    case nameof(Info):
                        {
                            if (args.Length == 2 && !args[1].StartsWith('-'))
                            {
                                var clientId = GetClientId();
                                await Info(clientId, args[1]);
                            }
                            else
                            {
                                ParseInfoArgs(args.Skip(1), out var clientId, out var sortBy, out var checkNamechanges);
                                await Info(clientId, sortBy, checkNamechanges);
                            }
                        }
                        break;

                    case nameof(BanTool):
                        {
                            ParseBanToolArgs(args.Skip(1), out var login, out var token, out var channel, out var command, out var commandArgs, out var limit, out var period);
                            await BanTool(login, token, channel, command, commandArgs, limit, period);
                        }
                        break;

                    default:
                        Error($"Invalid module name: \"{args[0]}\".");
                        break;
                }
            }
            else
            {
                PrintUsage();
            }
        }

        static void Error(string message)
        {
            Console.Error.WriteLine($"Error: {message}\n");
            Environment.Exit(1);
        }
        static void PrintUsage()
        {
            Console.WriteLine(
$@"Usage: {AppDomain.CurrentDomain.FriendlyName} [MODULE] [OPTION]...

    Modules: {nameof(Followers)}, {nameof(Following)}
    Arguments: <channel>
    Options:
        -l, --limit
                number of users to fetch (default: {DefaultFollowLimit})
        -o, --offset
                starting offset
        -d, --direction (default: {DefaultFollowDirection})
                'asc' for ascending order or 'desc' for descending

    Module: {nameof(Info)}
    Arguments: [username]
    Options:
        -d, --date
                sort users by date of creation
        -n, --name
                sort users by name
        -c
                check namechanges

    Module: {nameof(BanTool)}
    Arguments: <channel>
    Options:
        -c, --command
                command (default: {DefaultBantoolCommand})
        -a, --args
                command args
        -l, --limit
                maximum number of actions per period (default: {DefaultBantoolLimit})
        -p, --period
                period (seconds) in which a limited number of actions can be performed (default: {DefaultBantoolPeriod})
            --login
                login username (default: {EnvLogin} environment variable)
            --token
                oauth token (default: {EnvToken} environment variable)"
            );
        }

        static Dictionary<string, string> MapArgs(IEnumerable<string> args)
        {
            var res = new Dictionary<string, string>();

            if (!(args is IList<string> list))
                list = args.ToList();

            for (int i = 0; i < list.Count; i++)
            {
                var key = list[i];
                if (key.Length == 2 && key[0] == '-' && key[1] != '-')
                    key = key.Substring(1);
                else if (key.StartsWith("--"))
                    key = key.Substring(2);
                else
                    throw new ArgumentException($"Unknown option: {list[i]}");

                string value = null;
                if (i < list.Count - 1)
                {
                    var nextArg = list[i + 1];
                    if (nextArg.Length == 0 || nextArg[0] != '-')
                    {
                        value = nextArg;
                        i++;
                    }
                }
                res[key] = value;
            }

            return res;
        }

        static string GetClientId()
        {
            var clientId = Environment.GetEnvironmentVariable(EnvClientId);
            if (clientId == null)
                Error($"Missing client ID ({EnvClientId} environment variable)");

            return clientId;
        }
        static void ParseFollowArgs(IEnumerable<string> args, out string clientId, out string userId, out int limit, out int offset, out string direction)
        {
            limit = DefaultFollowLimit;
            offset = 0;
            direction = DefaultFollowDirection;
            clientId = GetClientId();

            var username = args.FirstOrDefault();
            if (username?.StartsWith('-') != false)
                Error("Missing channel name.");

            using (var client = new HelixApiClient(clientId))
            {
                var res = client.GetUsersAsync(new GetUsersParams { UserLogins = new[] { username } }).GetAwaiter().GetResult();
                var user = res.Data.FirstOrDefault();

                if (user == null)
                    Error($"Could not find channel: {username}.");

                userId = user.Id;
            }

            args = args.Skip(1);

            var dict = MapArgs(args);
            foreach (var (k, v) in dict)
            {
                switch (k)
                {
                    case "limit":
                    case "l":
                        if (!int.TryParse(v, out limit) || limit <= 0)
                            Error($"Option \"{k}\" must have an integer value greater than 0.");
                        break;

                    case "offset":
                    case "o":
                        if (!int.TryParse(v, out offset) || offset < 0)
                            Error($"Option \"{k}\" must have an integer value greater than 0.");
                        break;

                    case "direction":
                    case "d":
                        if (string.Equals(v, "asc", StringComparison.OrdinalIgnoreCase))
                            direction = "asc";
                        else if (string.Equals(v, "desc", StringComparison.OrdinalIgnoreCase))
                            direction = "desc";
                        else
                            Error($"Option \"{k}\" must have a value of either \"desc\" or \"asc\".");
                        break;

                    default:
                        Error($"Invalid option: \"{k}\".");
                        break;
                }
            }
        }
        static void ParseInfoArgs(IEnumerable<string> args, out string clientId, out string sortBy, out bool? checkNamechanges)
        {
            sortBy = null;
            checkNamechanges = null;
            clientId = GetClientId();

            var dict = MapArgs(args);
            foreach (var (k, v) in dict)
            {
                switch (k)
                {
                    case "date":
                    case "d":
                        if (v != null)
                            Error($"Option \"{k}\" does not accept a value.");
                        sortBy = "date";
                        break;

                    case "name":
                    case "n":
                        if (v != null)
                            Error($"Option \"{k}\" does not accept a value.");
                        sortBy = "name";
                        break;

                    case "namechanges":
                    case "c":
                        if (!bool.TryParse(v, out var res))
                            Error($"Option \"{k}\" must have a bool value.");
                        checkNamechanges = res;
                        break;

                    default:
                        Error($"Invalid option: \"{k}\".");
                        break;
                }
            }
        }
        static void ParseBanToolArgs(IEnumerable<string> args, out string login, out string token, out string channelname, out string command, out string commandArgs, out int limit, out int period)
        {
            command = DefaultBantoolCommand;
            commandArgs = null;
            login = null;
            token = null;
            limit = DefaultBantoolLimit;
            period = DefaultBantoolPeriod;

            var firstArg = args.FirstOrDefault();
            if (firstArg?.StartsWith('-') != false)
                Error("Missing channel name.");
            channelname = firstArg;
            args = args.Skip(1);

            var dict = MapArgs(args);
            foreach (var (k, v) in dict)
            {
                switch (k)
                {
                    case "limit":
                    case "l":
                        if (!int.TryParse(v, out limit) || limit <= 0)
                            Error($"Option \"{k}\" must have an integer value greater than 0.");
                        break;

                    case "period":
                    case "p":
                        if (!int.TryParse(v, out period) || period <= 0)
                            Error($"Option \"{k}\" must have an integer value greater than 0.");
                        break;

                    case "command":
                    case "c":
                        if (v == null)
                            Error($"Option \"{k}\" must have a value.");
                        command = v;
                        break;

                    case "args":
                    case "a":
                        if (v == null)
                            Error($"Option \"{k}\" must have a value.");
                        commandArgs = v;
                        break;

                    case "login":
                        if (v == null)
                            Error($"Option \"{k}\" must have a value.");
                        login = v;
                        break;

                    case "token":
                        if (v == null)
                            Error($"Option \"{k}\" must have a value.");
                        token = v;
                        break;

                    default:
                        Error($"Invalid option: \"{k}\".");
                        break;
                }
            }

            login ??= Environment.GetEnvironmentVariable(EnvLogin);
            token ??= Environment.GetEnvironmentVariable(EnvToken);

            if (login == null)
                Error($"Missing login username ({EnvLogin} environment variable or --login option)");
            if (token == null)
                Error($"Missing token ({EnvToken} environment variable or --token option");
        }

        static int GetLimit(int count, int totalLimit, int maxLimit = 100)
        {
            var res = totalLimit - count;
            if (res > maxLimit)
                res = maxLimit;

            return res;
        }
        static List<string> GetUsernames()
        {
            if (!Console.IsInputRedirected)
                Console.WriteLine("Enter usernames separated by non-word characters or new lines:");

            var result = new List<string>();
            string login;
            while (!string.IsNullOrWhiteSpace(login = Console.ReadLine()))
                result.AddRange(Regex.Split(login.ToLower(), @"\W+"));

            return result.Distinct().Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
        }
        static async Task PaginatedRequest<T>(Func<Task<T>> request, Func<T, Task<T>> nextRequest, Action<T> perform, Func<T, bool> condition)
        {
            var result = await request();
            perform?.Invoke(result);

            while (condition(result))
            {
                result = await nextRequest(result);
                perform?.Invoke(result);
            }
        }
    }
}
