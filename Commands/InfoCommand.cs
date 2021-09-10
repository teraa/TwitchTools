using System;
using System.Linq;
using System.Threading.Tasks;
using Twitch.Rest.Helix;

namespace TwitchTools.Commands
{

    public class InfoCommand : ICommand
    {

        public string User { get; set; }
        public bool IsId { get; set; }
        public string ClientId { get; set; }
        public string Token { get; set; }

        public async Task RunAsync()
        {
            if (ClientId is null)
                Program.Error("Client ID not set.");

            if (Token is null)
                Program.Error("Token not set.");

            var client = new TwitchRestClient(ClientId, Token);

            var args = IsId
                ? new GetUsersArgs { Ids = new[] { User } }
                : new GetUsersArgs { Logins = new[] { User } };

            var res = await client.GetUsersAsync(args);
            var user = res.Data.FirstOrDefault();

            if (user is null)
                Program.Error($"Could not find user: {User}");

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
    }
}
