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

            Console.WriteLine(string.Join(',', new[]
                {
                    "#",
                    "Followed at (UTC)",
                    "ID",
                    "DisplayName",
                }
            ));

            string countFormat = $"d{(int)Math.Ceiling(Math.Log10(args.Limit + 1))}";

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
                        $"{(++count).ToString(countFormat)}",
                        follow.FollowedAt.ToString(Program.TimestampFormat),
                        follow.ToId,
                        follow.ToName,
                    },
                FollowOrigin.To
                    => follow => new List<string>
                    {
                        $"{(++count).ToString(countFormat)}",
                        follow.FollowedAt.ToString(Program.TimestampFormat),
                        follow.FromId,
                        follow.FromName,
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

                foreach (var follow in response.Data)
                    Console.WriteLine(string.Join(',', dataSelector(follow)));

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