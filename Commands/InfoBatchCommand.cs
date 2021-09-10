using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Twitch.Rest.Helix;
using TwitchTools.Utils;

namespace TwitchTools.Commands
{
    public class InfoBatchCommand : ICommand
    {
        private const int BatchLimit = 100;

        public IEnumerable<string> Users { get; set; }
        public bool IsId { get; set; }
        public InfoSort? SortBy { get; set; }
        public string ClientId { get; set; }
        public string Token { get; set; }

        public enum InfoSort
        {
            Date,
            Name
        }

        public async Task RunAsync()
        {
            // TODO: validate clientid, token not null

            Users ??= ConsoleUtils.GetInputList("Enter users:", @"\W+")
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(x => !string.IsNullOrWhiteSpace(x));

            IEnumerable<string> remainingUsers = Users.ToList();
            var retrievedUsers = new List<User>();

            var client = new TwitchRestClient(ClientId, Token);

            while (remainingUsers.Any())
            {
                var batch = remainingUsers.Take(BatchLimit).ToArray();
                var args = IsId
                    ? new GetUsersArgs { Ids = batch }
                    : new GetUsersArgs { Logins = batch };

                var response = await client.GetUsersAsync(args);
                retrievedUsers.AddRange(response.Data);
                remainingUsers = remainingUsers.Skip(BatchLimit);
            }

            retrievedUsers = SortBy switch
            {
                InfoSort.Date => retrievedUsers.OrderBy(x => x.CreatedAt).ToList(),
                InfoSort.Name => retrievedUsers.OrderBy(x => x.Login).ToList(),
                _ => retrievedUsers,
            };

            Console.WriteLine(string.Join(',', new[]
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

                Console.WriteLine(string.Join(',', data));
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
        }
    }
}
