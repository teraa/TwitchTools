using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Twitch.API.Kraken;
using Twitch.API.Kraken.Rest;
using TwitchTools.Utils;

namespace TwitchTools
{
    partial class Program
    {
        public enum InfoModuleSort
        {
            Date,
            Name
        }

        static async Task Info(string clientId, string username)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            using var client = new KrakenApiClient(clientId);
#pragma warning restore CS0618 // Type or member is obsolete
            var res = await client.GetUsersAsync(new GetUsersParams { UserLogins = new[] { username } });
            var user = res.Users.FirstOrDefault();

            if (user == null)
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

        static async Task Info(string clientId, InfoModuleSort? sortBy, bool? checkNamechanges)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            using var client = new KrakenApiClient(clientId);
#pragma warning restore CS0618 // Type or member is obsolete

            var logins = ConsoleUtils.GetInputList("Enter usernames:", @"\W+")
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(x => !string.IsNullOrWhiteSpace(x));

            IEnumerable<string> remainingUsers = logins.ToList();
            var retrievedUsers = new List<User>();

            while (remainingUsers.Any())
            {
                var requestParams = new GetUsersParams { UserLogins = remainingUsers.Take(DefaultRequestLimit) };
                var response = await client.GetUsersAsync(requestParams);
                retrievedUsers.AddRange(response.Users);
                remainingUsers = remainingUsers.Skip(DefaultRequestLimit);
            }

            retrievedUsers = sortBy switch
            {
                InfoModuleSort.Date => retrievedUsers.OrderBy(x => x.CreatedAt).ToList(),
                InfoModuleSort.Name => retrievedUsers.OrderBy(x => x.Login).ToList(),
                null => retrievedUsers,
                _ => throw new ArgumentException("Invalid argument value", nameof(sortBy))
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

            var missing = logins.Except(retrievedUsers.Select(x => x.Login)).ToList();
            if (!missing.Any())
                return;

            Console.WriteLine($"{missing.Count} users not found:\n");
            foreach (var user in missing)
                Console.WriteLine(user);

            checkNamechanges ??= ConsoleUtils.GetAnswer($"\nCheck namechanges?", false);

            if (checkNamechanges == false)
                return;

            throw new NotImplementedException();
        }
    }
}
