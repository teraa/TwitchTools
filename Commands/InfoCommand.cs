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

            GetResponse<User> response;
            try
            {
                response = await client.GetUsersAsync(args);
            }
            catch (Exception ex)
            {
                Error(ex.Message);
                return 1;
            }

            var user = response.Data.FirstOrDefault();
            if (user is null)
            {
                Error($"Could not find user: {User}");
                return 1;
            }

            var data = new (string title, string value)[]
            {
                ("ID", user.Id),
                ("Login", user.Login),
                ("Display Name", user.DisplayName),
                ("Type", user.Type),
                ("Broadcaster Type", user.BroadcasterType),
                ("Description", user.Description),
                ("Created at (UTC)", user.CreatedAt.ToString(Program.TimestampFormat)),
                ("View Count", user.ViewCount.ToString()),
                ("Profile Image", user.ProfileImageUrl),
                ("Offline Image", user.OfflineImageUrl),
            };

            var maxLength = data.Select(x => x.title.Length).Max();

            foreach (var (title, value) in data)
                Console.WriteLine(title.PadRight(maxLength + 1) + value);

            return 0;
        }
    }
}
