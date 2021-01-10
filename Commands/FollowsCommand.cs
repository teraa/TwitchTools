using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Twitch.Rest.Helix;
using TwitchTools.Utils;

namespace TwitchTools
{
    public static class FollowsCommand
    {
        private const int BatchLimit = 100;

        public static async Task RunAsync(Args args)
        {
            var clientId = Program.GetEnvironmentVariableOrError(Program.EnvClientId);
            var token = Program.GetEnvironmentVariableOrError(Program.EnvToken);
            var client = new TwitchRestClient(clientId, token);

            string userId;
            if (args.IsId)
            {
                userId = args.User;
            }
            else
            {
                var response = await client.GetUsersAsync(new GetUsersArgs { Logins = new[] { args.User } });
                var restUser = response.Data.FirstOrDefault();
                if (restUser is null)
                    Program.Error($"Could not find user: {args.User}");

                userId = restUser.Id;
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

            (string fromId, string toId) user = args.Origin switch
            {
                FollowOrigin.From => (userId, null),
                FollowOrigin.To => (null, userId),
                _ => throw new ArgumentOutOfRangeException(nameof(args.Origin))
            };

            int count = 0;
            var firstRequestArgs = new GetFollowsArgs
            {
                FromId = user.fromId,
                ToId = user.toId,
                After = args.Cursor,
                First = GetNextLimit(count, args.Limit),
            };

            Func<Follow, List<string>> dataSelector = args.Origin switch
            {
                FollowOrigin.From
                    => follow => new List<string>
                    {
                        $"{(++count)}:",
                        follow.FollowedAt.ToString(Program.TimestampFormat),
                        follow.ToName,
                        follow.ToId
                    },
                FollowOrigin.To
                    => follow => new List<string>
                    {
                        $"{(++count)}:",
                        follow.FollowedAt.ToString(Program.TimestampFormat),
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
                return client.GetFollowsAsync(firstRequestArgs);
            }
            Task<GetResponse<Follow>> NextRequest(GetResponse<Follow> prev)
            {
                var newArgs = new GetFollowsArgs
                {
                    FromId = firstRequestArgs.FromId,
                    ToId = firstRequestArgs.ToId,
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
            return result > BatchLimit ? BatchLimit : result;
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

        public class Args
        {
            public FollowOrigin Origin { get; set; }
            public string User { get; set; }
            public bool IsId { get; set; }
            public int Limit { get; set; }
            public string Cursor { get; set; }
        }

        public enum FollowOrigin
        {
            From,
            To
        }

    }
}