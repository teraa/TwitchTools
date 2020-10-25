using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Twitch.API.Kraken;
using Twitch.API.Kraken.Params;
using Twitch.API.Kraken.Responses;
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
            var clientId = GetEnvironmentVariableOrError(EnvTokenClientId);
            var token = GetEnvironmentVariableOrError(EnvToken);
            using var client = new Twitch.API.Helix.HelixApiClient(clientId, token);
            var res = await client.GetUsersAsync(new Twitch.API.Helix.Params.GetUsersParams { UserLogins = new[] { username } });
            var user = res.Data.FirstOrDefault();

            if (user == null)
                Error($"Could not find channel: {username}.");

            return user.Id;
        }

        public class FollowersArguments
        {
            public string Channel { get; set; }
            public int Limit { get; set; }
            public int Offset { get; set; }
            public Direction Direction { get; set; }
            public string Cursor { get; set; }
        }

        static async Task Followers(FollowersArguments args)
        {
            var userId = await GetUserId(args.Channel);
            var clientId = GetEnvironmentVariableOrError(EnvClientId);

#pragma warning disable CS0618 // Type or member is obsolete
            using var client = new KrakenApiClient(clientId);
#pragma warning restore CS0618 // Type or member is obsolete

            var tableHeaders = new List<TableHeader>
            {
                new TableHeader("", (int)Math.Ceiling(Math.Log10(args.Limit + 1)) + 1),
                new TableHeader("Followed at (UTC)", -19),
                new TableHeader("Name", -25),
                new TableHeader("Display Name", -25),
                new TableHeader("ID", 15),
                new TableHeader("Created at (UTC)", -19),
            };
            var tableOptions = new TablePrintOptions
            {
                Borders = TableBorders.None
            };
            TableUtils.PrintHeaders(tableHeaders, tableOptions);

            int count = 0;
            var requestParams = new GetChannelFollowersParams
            {
                Direction = args.Direction.ToString().ToLower(),
                Limit = GetNextLimit(count, args.Limit),
                Offset = args.Offset,
                Cursor = args.Cursor
            };

            await PaginatedRequest(Request, NextRequest, Perform, Condition);

            if (Console.IsOutputRedirected)
                Console.WriteLine($"cursor: {args.Cursor}");
            else
                Console.WriteLine();


            Task<GetChannelFollowersResponse> Request()
            {
                return client.GetChannelFollowersAsync(userId, requestParams);
            }
            Task<GetChannelFollowersResponse> NextRequest(GetChannelFollowersResponse prev)
            {
                requestParams.Cursor = prev.Cursor;
                requestParams.Limit = GetNextLimit(count, args.Limit);
                requestParams.Offset = 0;
                return client.GetChannelFollowersAsync(userId, requestParams);
            }
            void Perform(GetChannelFollowersResponse response)
            {
                args.Cursor = response.Cursor;

                if (!Console.IsOutputRedirected)
                    Console.Write("\r");
                foreach (var follow in response.Follows)
                {
                    var rowData = new List<string> { $"{(++count)}:", follow.CreatedAt.ToString(TimestampFormat), follow.User.Login, follow.User.DisplayName, follow.User.Id, follow.User.CreatedAt.ToString(TimestampFormat) };
                    var row = new TableRow(rowData);
                    TableUtils.PrintRow(tableHeaders, row, tableOptions);
                }
                if (!Console.IsOutputRedirected)
                    Console.Write($"cursor: {response.Cursor}");
            }
            bool Condition(GetChannelFollowersResponse res)
            {
                return !string.IsNullOrEmpty(res.Cursor) && count < args.Limit;
            }

        }

        public class FollowingArguments
        {
            public string Channel { get; set; }
            public int Limit { get; set; }
            public int Offset { get; set; }
            public Direction Direction { get; set; }
        }

        static async Task Following(FollowingArguments args)
        {
            var userId = await GetUserId(args.Channel);
            var clientId = GetEnvironmentVariableOrError(EnvClientId);

#pragma warning disable CS0618 // Type or member is obsolete
            using var client = new KrakenApiClient(clientId);
#pragma warning restore CS0618 // Type or member is obsolete

            var tableHeaders = new List<TableHeader>
            {
                new TableHeader("", (int)Math.Ceiling(Math.Log10(args.Limit + 1)) + 1),
                new TableHeader("Followed at (UTC)", -19),
                new TableHeader("Name", -25),
                new TableHeader("Display Name", -25),
                new TableHeader("ID", 15),
                new TableHeader("Created at (UTC)", -19),
            };
            var tableOptions = new TablePrintOptions
            {
                Borders = TableBorders.None
            };
            TableUtils.PrintHeaders(tableHeaders, tableOptions);

            int count = 0;
            var requestParams = new GetUserFollowsParams
            {
                Direction = args.Direction.ToString().ToLower(),
                Limit = GetNextLimit(count, args.Limit),
                Offset = args.Offset
            };

            await PaginatedRequest(Request, NextRequest, Perform, Condition);

            Task<GetUserFollowsResponse> Request()
            {
                return client.GetUserFollowsAsync(userId, requestParams);
            }
            Task<GetUserFollowsResponse> NextRequest(GetUserFollowsResponse prev)
            {
                requestParams.Offset += requestParams.Limit;
                requestParams.Limit = GetNextLimit(count, args.Limit);
                return client.GetUserFollowsAsync(userId, requestParams);
            }
            void Perform(GetUserFollowsResponse response)
            {
                foreach (var follow in response.Follows)
                {
                    var rowData = new List<string> { $"{(++count)}:", follow.CreatedAt.ToString(TimestampFormat), follow.Channel.Login, follow.Channel.DisplayName, follow.Channel.Id, follow.Channel.CreatedAt.ToString(TimestampFormat) };
                    var row = new TableRow(rowData);
                    TableUtils.PrintRow(tableHeaders, row, tableOptions);
                }
            }
            bool Condition(GetUserFollowsResponse res)
            {
                return res.Follows.Any() && count < args.Limit;
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
