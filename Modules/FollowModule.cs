using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Twitch.API.KrakenV5;
using Twitch.API.KrakenV5.Rest;
using TwitchTools.Utils;

namespace TwitchTools
{
    partial class Program
    {
        static async Task Followers(string clientId, string userId, int limit, int offset, string direction)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            using var client = new RestApiClient(clientId);
#pragma warning restore CS0618 // Type or member is obsolete

            var tableHeaders = new List<TableHeader>
            {
                new TableHeader("", (int)Math.Ceiling(Math.Log10(limit + 1)) + 1),
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
            var requestParams = new GetChannelFollowersParams { Direction = direction, Limit = GetLimit(count, limit), Offset = offset };

            await PaginatedRequest(Request, NextRequest, Perform, Condition);

            Task<GetChannelFollowersResponse> Request()
            {
                return client.GetChannelFollowersAsync(userId, requestParams);
            }
            Task<GetChannelFollowersResponse> NextRequest(GetChannelFollowersResponse prev)
            {
                requestParams.Cursor = prev.Cursor;
                requestParams.Limit = GetLimit(count, limit);
                return client.GetChannelFollowersAsync(userId, requestParams);
            }
            void Perform(GetChannelFollowersResponse response)
            {
                foreach (var follow in response.Follows)
                {
                    var rowData = new List<string> { $"{(++count)}:", follow.CreatedAt.ToString(TimestampFormat), follow.User.Login, follow.User.DisplayName, follow.User.Id, follow.User.CreatedAt.ToString(TimestampFormat) };
                    var row = new TableRow(rowData);
                    TableUtils.PrintRow(tableHeaders, row, tableOptions);
                }
            }
            bool Condition(GetChannelFollowersResponse res)
            {
                return !string.IsNullOrEmpty(res.Cursor) && count < limit;
            }

        }

        static async Task Following(string clientId, string userId, int limit, int offset, string direction)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            using var client = new RestApiClient(clientId);
#pragma warning restore CS0618 // Type or member is obsolete

            var tableHeaders = new List<TableHeader>
            {
                new TableHeader("", (int)Math.Ceiling(Math.Log10(limit + 1)) + 1),
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
            var requestParams = new GetUserFollowsParams { Direction = direction, Limit = GetLimit(count, limit), Offset = offset };

            await PaginatedRequest(Request, NextRequest, Perform, Condition);

            Task<GetUserFollowsResponse> Request()
            {
                return client.GetUserFollowsAsync(userId, requestParams);
            }
            Task<GetUserFollowsResponse> NextRequest(GetUserFollowsResponse prev)
            {
                requestParams.Limit = GetLimit(count, limit);
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
                return res.Follows.Any() && count < limit;
            }
        }
    }
}
