using Sevriukoff.AsciiArena.CommonLib;

namespace Sevriukoff.AsciiArena.Client;

public interface INetworkClient : IDisposable
{
    Task ConnectAsync();
    
    Task SendCommandAsync(CommandMessage command);
    
    event Action<GameState?> OnGameStateReceived;
}