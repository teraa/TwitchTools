using System;
using System.Linq;
using System.Threading.Tasks;
using Twitch.Rest.Helix;
using static TwitchTools.ConsoleUtils;

namespace TwitchTools.Commands
{

    public class InfoCommand : ICommand
    {

        // Arg
        public string User { get; set; } = null!;
        // Opt
        public bool IsId { get; set; }
        public string? ClientId { get; set; }
        public string? Token { get; set; }

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

            var args = IsId
                ? new GetUsersArgs { Ids = new[] { User } }
                : new GetUsersArgs { Logins = new[] { User } };

            var res = await client.GetUsersAsync(args);
            var user = res!.Data.FirstOrDefault();

            if (user is null)
            {
                Error($"Could not find user: {User}");
                return 1;
            }

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

            return 0;
        }
    }
}
