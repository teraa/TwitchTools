using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Twitch.Rest.Helix;
using static TwitchTools.ConsoleUtils;

namespace TwitchTools.Commands
{
    public class InfoBatchCommand : ICommand
    {
        private const int s_batchLimit = 100;
        private const char s_separator = ',';

        // Arg
        public IEnumerable<string>? Users { get; set; }
        // Opt
        public bool IsId { get; set; }
        public InfoSort SortBy { get; set; }
        public string? ClientId { get; set; }
        public string? Token { get; set; }

        public enum InfoSort
        {
            None,
            Date,
            Name
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

            Users ??= GetInputList("Enter users:", @"\W+")
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(x => !string.IsNullOrWhiteSpace(x));

            IEnumerable<string> remainingUsers = Users.ToList();
            var retrievedUsers = new List<User>();

            var client = new TwitchRestClient(ClientId, Token);

            while (remainingUsers.Any())
            {
                var batch = remainingUsers.Take(s_batchLimit).ToArray();
                var args = IsId
                    ? new GetUsersArgs { Ids = batch }
                    : new GetUsersArgs { Logins = batch };

                GetResponse<User> response;
                try
                {
                    response = await client.GetUsersAsync(args, cancellationToken);
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

                retrievedUsers.AddRange(response.Data);
                remainingUsers = remainingUsers.Skip(s_batchLimit);
            }

            retrievedUsers = SortBy switch
            {
                InfoSort.None => retrievedUsers,
                InfoSort.Date => retrievedUsers.OrderBy(x => x.CreatedAt).ToList(),
                InfoSort.Name => retrievedUsers.OrderBy(x => x.Login).ToList(),
                _ => throw new ArgumentOutOfRangeException(nameof(SortBy), SortBy, "Unknown value."),
            };

            Console.WriteLine(string.Join(s_separator, new[]
                {
                    "Created At (UTC)",
                    "ID",
                    "Login",
                    "Display Name",
                }
            ));

            foreach (var user in retrievedUsers)
            {
                var data = new[]
                {
                    user.CreatedAt.ToString(Program.TimestampFormat),
                    user.Id,
                    user.Login,
                    user.DisplayName,
                };

                Console.WriteLine(string.Join(s_separator, data));
            }

            Func<User, string> selector = IsId
                ? x => x.Id
                : x => x.Login;

            var missing = Users.Except(
                retrievedUsers.Select(selector),
                StringComparer.OrdinalIgnoreCase).ToList();
            if (missing.Any())
            {
                Console.WriteLine($"{missing.Count} users not found:\n");
                foreach (var user in missing)
                    Console.WriteLine(user);
            }

            return 0;
        }
    }
}
