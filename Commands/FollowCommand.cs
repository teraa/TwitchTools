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
        public enum Direction
        {
            Asc,
            Desc
        }

        static async Task<string> GetUserId(string username)
        {
            var clientId = GetEnvironmentVariableOrError(EnvClientId);
            var token = GetEnvironmentVariableOrError(EnvToken);
            var client = new TwitchRestClient(clientId, token);

            var response = await client.GetUsersAsync(new GetUsersArgs { Logins = new[] { username } });
            var user = response.Data.FirstOrDefault();

            if (user == null)
                Error($"Could not find channel: {username}.");

            return user.Id;
        }

        public class FollowersArguments
        {
            public string Channel { get; set; }
            public int Limit { get; set; }
            public string Cursor { get; set; }
        }

        static async Task Followers(FollowersArguments args)
        {
            var userId = await GetUserId(args.Channel);
            var clientId = GetEnvironmentVariableOrError(EnvClientId);
            var token = GetEnvironmentVariableOrError(EnvToken);
            var client = new TwitchRestClient(clientId, token);

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
                ToId = userId,
                After = args.Cursor,
                First = GetNextLimit(count, args.Limit),
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
                    ToId = userId,
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
                    var rowData = new List<string> { $"{(++count)}:", follow.FollowedAt.ToString(TimestampFormat), follow.FromName, follow.FromId };
                    var row = new TableRow(rowData);
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

        public class FollowingArguments
        {
            public string Channel { get; set; }
            public int Limit { get; set; }
        }

        static async Task Following(FollowingArguments args)
        {
            var userId = await GetUserId(args.Channel);
            var clientId = GetEnvironmentVariableOrError(EnvClientId);
            var token = GetEnvironmentVariableOrError(EnvToken);
            var client = new TwitchRestClient(clientId, token);

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
                FromId = userId,
                First = GetNextLimit(count, args.Limit),
            };

            await PaginatedRequest(Request, NextRequest, Perform, Condition);

            Task<GetResponse<Follow>> Request()
            {
                return client.GetFollowsAsync(requestArgs);
            }
            Task<GetResponse<Follow>> NextRequest(GetResponse<Follow> prev)
            {
                var newArgs = new GetFollowsArgs
                {
                    FromId = userId,
                    After = prev.Pagination?.Cursor,
                    First = GetNextLimit(count, args.Limit),
                };

                return client.GetFollowsAsync(newArgs);
            }
            void Perform(GetResponse<Follow> response)
            {
                foreach (var follow in response.Data)
                {
                    var rowData = new List<string> { $"{(++count)}:", follow.FollowedAt.ToString(TimestampFormat), follow.ToName, follow.ToId };
                    var row = new TableRow(rowData);
                    TableUtils.PrintRow(tableHeaders, row, tableOptions);
                }
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
