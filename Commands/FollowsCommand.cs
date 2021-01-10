using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Twitch.Rest.Helix;
using TwitchTools.Utils;

namespace TwitchTools
{
    partial class Program
    {
        public class FollowsCommandArgs
        {
            public FollowOrigin Origin { get; set; }
            public string User { get; set; }
            public int Limit { get; set; }
            public string Cursor { get; set; }
        }

        public enum FollowOrigin
        {
            FromId,
            From,
            ToId,
            To
        }

        static async Task Follows(FollowsCommandArgs args)
        {
            var clientId = GetEnvironmentVariableOrError(EnvClientId);
            var token = GetEnvironmentVariableOrError(EnvToken);
            var client = new TwitchRestClient(clientId, token);

            (string toId, string fromId) user = args.Origin switch
            {
                FollowOrigin.FromId => (null, args.User),
                FollowOrigin.From => (null, await GetUserId(args.User)),
                FollowOrigin.ToId => (args.User, null),
                FollowOrigin.To => (await GetUserId(args.User), null),
                _ => throw new ArgumentOutOfRangeException(nameof(args.Origin)),
            };

            async Task<string> GetUserId(string login)
            {
                var response = await client.GetUsersAsync(new GetUsersArgs { Logins = new[] { login } });
                var user = response.Data.FirstOrDefault();
                if (user is null)
                    Error($"Could not find user: {login}");

                return user.Id;
            }

            var tableHeaders = new List<TableHeader>
            {
                new TableHeader("", (int)Math.Ceiling(Math.Log10(args.Limit + 1)) + 1),
                new TableHeader("Followed at (UTC)", -19),
                new TableHeader("Display Name", -25),
                new TableHeader("ID", 15),
            };
            var tableOptions = new TablePrintOptions
            {
                Borders = TableBorders.None
            };
            TableUtils.PrintHeaders(tableHeaders, tableOptions);

            int count = 0;
            var requestArgs = new GetFollowsArgs
            {
                ToId = user.toId,
                FromId = user.fromId,
                After = args.Cursor,
                First = GetNextLimit(count, args.Limit),
            };

            Func<Follow, List<string>> dataSelector = args.Origin switch
            {
                FollowOrigin.FromId or FollowOrigin.From
                    => follow => new List<string>
                    {
                        $"{(++count)}:",
                        follow.FollowedAt.ToString(TimestampFormat),
                        follow.ToName,
                        follow.ToId
                    },
                FollowOrigin.ToId or FollowOrigin.To
                    => follow => new List<string>
                    {
                        $"{(++count)}:",
                        follow.FollowedAt.ToString(TimestampFormat),
                        follow.FromName,
                        follow.FromId
                    },
                _ => throw new ArgumentOutOfRangeException(nameof(args.Origin))
            };

            await PaginatedRequest(Request, NextRequest, Perform, Condition);

            if (Console.IsOutputRedirected)
                Console.WriteLine($"cursor: {args.Cursor}");
            else
                Console.WriteLine();


            Task<GetResponse<Follow>> Request()
            {
                return client.GetFollowsAsync(requestArgs);
            }
            Task<GetResponse<Follow>> NextRequest(GetResponse<Follow> prev)
            {
                var newArgs = new GetFollowsArgs
                {
                    ToId = user.toId,
                    FromId = user.fromId,
                    After = prev.Pagination?.Cursor,
                    First = GetNextLimit(count, args.Limit),
                };

                return client.GetFollowsAsync(newArgs);
            }
            void Perform(GetResponse<Follow> response)
            {
                args.Cursor = response.Pagination?.Cursor;

                if (!Console.IsOutputRedirected)
                    Console.Write("\r");
                foreach (var follow in response.Data)
                {
                    var row = new TableRow(dataSelector(follow));
                    TableUtils.PrintRow(tableHeaders, row, tableOptions);
                }
                if (!Console.IsOutputRedirected)
                    Console.Write($"cursor: {response.Pagination?.Cursor}");
            }
            bool Condition(GetResponse<Follow> res)
            {
                return !string.IsNullOrEmpty(res.Pagination?.Cursor) && count < args.Limit;
            }

        }
        static int GetNextLimit(int count, int totalLimit)
        {
            var result = totalLimit - count;
            return result > DefaultRequestLimit ? DefaultRequestLimit : result;
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