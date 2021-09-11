using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Twitch.Rest.Helix;
using static TwitchTools.ConsoleUtils;

namespace TwitchTools.Commands
{
    public class FollowsCommand : ICommand
    {
        private const int BatchLimit = 100;

        // Arg
        public FollowOrigin Origin { get; set; }
        public string User { get; set; } = null!;
        // Opt
        public bool IsId { get; set; }
        public int? Limit { get; set; }
        public string? After { get; set; }
        public bool PrintCursor { get; set; }
        public string? ClientId { get; set; }
        public string? Token { get; set; }

        public enum FollowOrigin
        {
            From,
            To
        }

        public async Task<int> RunAsync()
        {
            if (ClientId is null)
            {
                Error("Client ID not set.");
                return 1;
            }

            if (Token is null)
            {
                Error("Token not set.");
                return 1;
            }

            var client = new TwitchRestClient(ClientId, Token);

            string userId;
            if (IsId)
            {
                userId = User;
            }
            else
            {
                var response = await client.GetUsersAsync(new GetUsersArgs { Logins = new[] { User } });
                var restUser = response!.Data.FirstOrDefault();
                if (restUser is null)
                {
                    Error($"Could not find user: {User}");
                    return 1;
                }

                userId = restUser!.Id;
            }

            Console.WriteLine(string.Join(',', new[]
                {
                    "#",
                    "Followed at (UTC)",
                    "ID",
                    "Login",
                    "DisplayName",
                }
            ));

            string countFormat = Limit is null
                ? "d"
                : $"d{(int)Math.Ceiling(Math.Log10(Limit.Value + 1))}";

            (string? fromId, string? toId) user = Origin switch
            {
                FollowOrigin.From => (userId, null),
                FollowOrigin.To => (null, userId),
                _ => throw new ArgumentOutOfRangeException(nameof(Origin))
            };

            int count = 0;
            string? lastCursor = After;

            var firstRequestArgs = new GetFollowsArgs
            {
                FromId = user.fromId,
                ToId = user.toId,
                After = lastCursor,
                First = GetNextLimit(count, Limit),
            };

            Func<Follow, List<string>> dataSelector = Origin switch
            {
                FollowOrigin.From
                    => follow => new List<string>
                    {
                        $"{(++count).ToString(countFormat)}",
                        follow.FollowedAt.ToString(Program.TimestampFormat),
                        follow.ToId,
                        follow.ToLogin,
                        follow.ToName,
                    },
                FollowOrigin.To
                    => follow => new List<string>
                    {
                        $"{(++count).ToString(countFormat)}",
                        follow.FollowedAt.ToString(Program.TimestampFormat),
                        follow.FromId,
                        follow.FromLogin,
                        follow.FromName,
                    },
                _ => throw new ArgumentOutOfRangeException(nameof(Origin))
            };

            try
            {
                await PaginatedRequest
                (
                    request: () => client.GetFollowsAsync(firstRequestArgs),
                    nextRequest: response =>
                    {
                        var newArgs = new GetFollowsArgs
                        {
                            FromId = firstRequestArgs.FromId,
                            ToId = firstRequestArgs.ToId,
                            After = lastCursor,
                            First = GetNextLimit(count, Limit),
                        };

                        return client.GetFollowsAsync(newArgs);
                    },
                    perform: response =>
                    {
                        lastCursor = response!.Pagination?.Cursor;

                        foreach (var follow in response.Data)
                            Console.WriteLine(string.Join(',', dataSelector(follow)));
                    },
                    condition: response => lastCursor is { Length: > 0 } && (Limit is null || count < Limit)
                );
            }
            catch
            {
                if (lastCursor is { Length: > 0 })
                    Console.WriteLine($"Last cursor: {lastCursor}");

                throw;
            }

            if (PrintCursor && lastCursor is { Length: > 0 })
                    Console.WriteLine($"Last cursor: {lastCursor}");

            return 0;
        }
        private static int GetNextLimit(int count, int? totalLimit)
        {
            if (totalLimit is null)
                return BatchLimit;

            var result = totalLimit.Value - count;

            return result > BatchLimit
                ? BatchLimit
                : result;
        }

        private static async Task PaginatedRequest<T>(Func<Task<T>> request, Func<T, Task<T>> nextRequest, Action<T> perform, Func<T, bool> condition)
        {
            var result = await request().ConfigureAwait(false);
            perform.Invoke(result);

            while (condition(result))
            {
                result = await nextRequest(result).ConfigureAwait(false);
                perform.Invoke(result);
            }
        }

    }
}