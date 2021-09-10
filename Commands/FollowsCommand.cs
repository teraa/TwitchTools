using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Twitch.Rest.Helix;

namespace TwitchTools.Commands
{
    public class FollowsCommand : ICommand
    {
        private const int BatchLimit = 100;

        public FollowOrigin Origin { get; set; }
        public string User { get; set; }
        public bool IsId { get; set; }
        public int Limit { get; set; }
        public string After { get; set; }
        public bool PrintCursor { get; set; }
        public string ClientId { get; set; }
        public string Token { get; set; }

        public enum FollowOrigin
        {
            From,
            To
        }

        public async Task RunAsync()
        {
            // TODO: validate clientid, token not null

            var client = new TwitchRestClient(ClientId, Token);

            string userId;
            if (IsId)
            {
                userId = User;
            }
            else
            {
                var response = await client.GetUsersAsync(new GetUsersArgs { Logins = new[] { User } });
                var restUser = response.Data.FirstOrDefault();
                if (restUser is null)
                    Program.Error($"Could not find user: {User}");

                userId = restUser.Id;
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

            string countFormat = $"d{(int)Math.Ceiling(Math.Log10(Limit + 1))}";

            (string fromId, string toId) user = Origin switch
            {
                FollowOrigin.From => (userId, null),
                FollowOrigin.To => (null, userId),
                _ => throw new ArgumentOutOfRangeException(nameof(Origin))
            };

            int count = 0;
            string lastCursor = After;

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
                await PaginatedRequest(Request, NextRequest, Perform, Condition);
            }
            catch
            {
                if (lastCursor is { Length: > 0 })
                    Console.WriteLine($"Last cursor: {lastCursor}");

                throw;
            }

            if (PrintCursor && lastCursor is { Length: > 0 })
                    Console.WriteLine($"Last cursor: {lastCursor}");

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
                    After = lastCursor,
                    First = GetNextLimit(count, Limit),
                };

                return client.GetFollowsAsync(newArgs);
            }
            void Perform(GetResponse<Follow> response)
            {
                lastCursor = response.Pagination?.Cursor;

                foreach (var follow in response.Data)
                    Console.WriteLine(string.Join(',', dataSelector(follow)));

            }
            bool Condition(GetResponse<Follow> res)
            {
                return lastCursor is { Length: > 0 } && count < Limit;
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

    }
}