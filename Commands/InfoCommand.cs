using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Twitch.API.Kraken;
using Twitch.API.Kraken.Params;
using TwitchTools.Utils;

namespace TwitchTools
{
    partial class Program
    {
        public enum InfoSort
        {
            None,
            Date,
            Name
        }

        static async Task Info(IEnumerable<string> username, InfoSort sort)
        {
            username ??= ConsoleUtils.GetInputList("Enter usernames:", @"\W+")
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(x => !string.IsNullOrWhiteSpace(x));

            if (username.Count() == 1)
                await InfoSingle(username.First());
            else
                await InfoMultiple(username, sort);
        }

        static async Task InfoSingle(string username)
        {
            var clientId = GetEnvironmentVariableOrError(EnvClientId);

#pragma warning disable CS0618 // Type or member is obsolete
            using var client = new KrakenApiClient(clientId);
#pragma warning restore CS0618 // Type or member is obsolete

            var res = await client.GetUsersAsync(new GetUsersParams { UserLogins = new[] { username } });
            var user = res.Users.FirstOrDefault();

            if (user is null)
                Error($"Could not find user: {username}");

            Console.WriteLine(
                $"ID:               {user.Id}\n" +
                $"Name:             {user.Login}\n" +
                $"Display Name:     {user.DisplayName}\n" +
                $"Type:             {user.Type}\n" +
                $"Description:      {user.Description}\n" +
                $"Created at (UTC): {user.CreatedAt.ToString(TimestampFormat)}\n" +
                $"Updated at (UTC): {user.UpdatedAt.ToString(TimestampFormat)}\n" +
                $"Profile Image:    {user.ProfileImageUrl}\n");
        }

        static async Task InfoMultiple(IEnumerable<string> usernames, InfoSort sort)
        {
            var clientId = GetEnvironmentVariableOrError(EnvClientId);

#pragma warning disable CS0618 // Type or member is obsolete
            using var client = new KrakenApiClient(clientId);
#pragma warning restore CS0618 // Type or member is obsolete

            IEnumerable<string> remainingUsers = usernames.ToList();
            var retrievedUsers = new List<User>();

            while (remainingUsers.Any())
            {
                var requestParams = new GetUsersParams { UserLogins = remainingUsers.Take(DefaultRequestLimit) };
                var response = await client.GetUsersAsync(requestParams);
                retrievedUsers.AddRange(response.Users);
                remainingUsers = remainingUsers.Skip(DefaultRequestLimit);
            }

            retrievedUsers = sort switch
            {
                InfoSort.Date => retrievedUsers.OrderBy(x => x.CreatedAt).ToList(),
                InfoSort.Name => retrievedUsers.OrderBy(x => x.Login).ToList(),
                InfoSort.None => retrievedUsers,
                _ => throw new ArgumentException("Invalid argument value", nameof(sort))
            };

            var tableHeaders = new List<TableHeader>
                {
                    new TableHeader("Name", -25),
                    new TableHeader("Display Name", -25),
                    new TableHeader("ID", 15),
                    new TableHeader("Created at (UTC)", -19),
                };

            var tableOptions = new TablePrintOptions
            {
                Borders = TableBorders.None
            };

            TableUtils.PrintHorizontalDivider(tableHeaders, tableOptions);
            TableUtils.PrintHeaders(tableHeaders, tableOptions);

            foreach (var user in retrievedUsers)
            {
                var rowData = new List<string> { user.Login, user.DisplayName, user.Id, user.CreatedAt.ToString(TimestampFormat) };
                var row = new TableRow(rowData);
                TableUtils.PrintRow(tableHeaders, row, tableOptions);
            }
            TableUtils.PrintHorizontalDivider(tableHeaders, tableOptions);

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
    }
}
