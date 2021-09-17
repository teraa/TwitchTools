using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Twitch.Rest.Helix;
using static TwitchTools.ConsoleUtils;

namespace TwitchTools.Commands
{
    public class FollowsCommand : ICommand
    {
        private const int s_batchLimit = 100;
        private const char s_separator = ',';

        // Arg
        public FollowOrigin Origin { get; set; }
        public string User { get; set; } = null!;
        // Opt
        public bool IsId { get; set; }
        public int? Limit { get; set; }
        public string? After { get; set; }
        public bool PrintCursor { get; set; }
        public bool LineNumbers { get; set; }
        public string? ClientId { get; set; }
        public string? Token { get; set; }
        public string? TimestampFormat { get; set; }

        public enum FollowOrigin
        {
            From,
            To
        }

        public async Task<int> RunAsync(CancellationToken cancellationToken)
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
                GetResponse<User> response;
                try
                {
                    response = await client.GetUsersAsync(new GetUsersArgs { Logins = new[] { User } }, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    return 1;
                }
                catch (Exception ex)
                {
                    Error(ex.Message);
                    return 1;
                }

                var restUser = response.Data.FirstOrDefault();
                if (restUser is null)
                {
                    Error($"Could not find user: {User}");
                    return 1;
                }

                userId = restUser.Id;
            }

            var headerItems = new List<string>
            {
                "Followed at (UTC)",
                "ID",
                "Login",
                "DisplayName",
            };

            if (LineNumbers)
                headerItems.Insert(0, "#");

            Console.WriteLine(string.Join(s_separator, headerItems));

            (string? fromId, string? toId) user;
            Func<Follow, List<string>> dataSelector;

            switch (Origin)
            {
                case FollowOrigin.From:
                    user = (fromId: userId, toId: null);
                    dataSelector = (follow) => new List<string>
                    {
                        follow.FollowedAt.ToString(TimestampFormat),
                        follow.ToId,
                        follow.ToLogin,
                        follow.ToName,
                    };
                    break;

                case FollowOrigin.To:
                    user = (fromId: null, toId: userId);
                    dataSelector = (follow) => new List<string>
                    {
                        follow.FollowedAt.ToString(TimestampFormat),
                        follow.FromId,
                        follow.FromLogin,
                        follow.FromName,
                    };
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(Origin), Origin, "Unknown value.");
            }

            GetResponse<Follow>? lastResponse = null;
            int retrieved = 0;

            try
            {
                int lineNumber = 0;
                int padding = Limit is null
                    ? 0
                    : (int)Math.Ceiling(Math.Log10(Limit.Value + 1));

                await PaginatedRequest
                (
                    request: () => client.GetFollowsAsync
                    (
                        args: new GetFollowsArgs
                        {
                            FromId = user.fromId,
                            ToId = user.toId,
                            After = After,
                            First = Limit is null
                                ? s_batchLimit
                                : Math.Min(s_batchLimit, Limit.Value - retrieved),
                        },
                        cancellationToken: cancellationToken
                    ),

                    nextRequest: (response) => client.GetFollowsAsync
                    (
                        args: new GetFollowsArgs
                        {
                            FromId = user.fromId,
                            ToId = user.toId,
                            After = response.Pagination?.Cursor,
                            First = Limit is null
                                ? s_batchLimit
                                : Math.Min(s_batchLimit, Limit.Value - retrieved),
                        },
                        cancellationToken: cancellationToken
                    ),

                    action: (response) =>
                    {
                        lastResponse = response;
                        retrieved += response.Data.Length;

                        if (LineNumbers)
                        {
                            foreach (var follow in response.Data)
                            {
                                lineNumber++;

                                var list = dataSelector(follow);
                                list.Insert(0, lineNumber.ToString().PadLeft(padding));

                                Console.WriteLine(string.Join(s_separator, list));
                            }
                        }
                        else
                        {
                            foreach (var follow in response.Data)
                                Console.WriteLine(string.Join(s_separator, dataSelector(follow)));
                        }
                    },

                    condition: (response) =>
                        response.Pagination?.Cursor?.Length > 0
                        && (Limit is null || retrieved < Limit)
                );
            }
            catch (OperationCanceledException)
            {
                return 1;
            }
            catch (Exception ex)
            {
                Error(ex.Message);
                PrintCursor = true;
                return 1;
            }
            finally
            {
                if (PrintCursor && lastResponse?.Pagination?.Cursor is { Length: > 0 } cursor)
                    Console.WriteLine($"Last cursor: {cursor}");
            }

            return 0;
        }

        private static async Task PaginatedRequest<T>(Func<Task<T>> request, Func<T, Task<T>> nextRequest, Action<T> action, Func<T, bool> condition)
        {
            var result = await request().ConfigureAwait(false);
            action.Invoke(result);

            while (condition(result))
            {
                result = await nextRequest(result).ConfigureAwait(false);
                action.Invoke(result);
            }
        }

    }
}