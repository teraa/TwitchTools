using System.Threading.Tasks;

namespace TwitchTools.Commands
{
    public interface ICommand
    {
        Task<int> RunAsync();
    }
}