using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Twitch.Rest.Helix;
using TwitchTools.Utils;

namespace TwitchTools
{
    public static class InfoCommand
    {
        private const int BatchLimit = 100;

        public static async Task RunAsync(IEnumerable<string> username, InfoSort sort)
        {
            username ??= ConsoleUtils.GetInputList("Enter usernames:", @"\W+")
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(x => !string.IsNullOrWhiteSpace(x));

            if (username.Count() == 1)
                await InfoSingle(username.First());
            else
                await InfoMultiple(username, sort);
        }

        private static async Task InfoSingle(string username)
        {
            var clientId = Program.GetEnvironmentVariableOrError(Program.EnvClientId);
            var token = Program.GetEnvironmentVariableOrError(Program.EnvToken);
            var client = new TwitchRestClient(clientId, token);

            var res = await client.GetUsersAsync(new GetUsersArgs { Logins = new[] { username } });
            var user = res.Data.FirstOrDefault();

            if (user is null)
                Program.Error($"Could not find user: {username}");

            Console.WriteLine
            (
                $"ID:               {user.Id}\n" +
                $"Login:            {user.Login}\n" +
                $"Display Name:     {user.DisplayName}\n" +
                $"Type:             {user.Type}\n" +
                $"Broadcaster Type: {user.BroadcasterType}\n" +
                $"Description:      {user.Description}\n" +
                $"Created at (UTC): {user.CreatedAt.ToString(Program.TimestampFormat)}\n" +
                $"View Count:       {user.ViewCount}\n" +
                $"Profile Image:    {user.ProfileImageUrl}\n" +
                $"Offline Image:    {user.OfflineImageUrl}\n"
            );
        }

        private static async Task InfoMultiple(IEnumerable<string> usernames, InfoSort sort)
        {
            var clientId = Program.GetEnvironmentVariableOrError(Program.EnvClientId);
            var token = Program.GetEnvironmentVariableOrError(Program.EnvToken);
            var client = new TwitchRestClient(clientId, token);

            IEnumerable<string> remainingUsers = usernames.ToList();
            var retrievedUsers = new List<User>();

            while (remainingUsers.Any())
            {
                var requestParams = new GetUsersArgs { Logins = remainingUsers.Take(BatchLimit).ToArray() };
                var response = await client.GetUsersAsync(requestParams);
                retrievedUsers.AddRange(response.Data);
                remainingUsers = remainingUsers.Skip(BatchLimit);
            }

            retrievedUsers = sort switch
            {
                InfoSort.Date => retrievedUsers.OrderBy(x => x.CreatedAt).ToList(),
                InfoSort.Name => retrievedUsers.OrderBy(x => x.Login).ToList(),
                InfoSort.None => retrievedUsers,
                _ => throw new ArgumentException("Invalid argument value", nameof(sort))
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

            var missing = usernames.Except(
                retrievedUsers.Select(x => x.Login),
                StringComparer.OrdinalIgnoreCase).ToList();
            if (missing.Any())
            {
                Console.WriteLine($"{missing.Count} users not found:\n");
                foreach (var user in missing)
                    Console.WriteLine(user);
            }
        }

        public enum InfoSort
        {
            None,
            Date,
            Name
        }

    }
}
