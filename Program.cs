using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Autofac;
using SadConsole;
using Game = SadConsole.Game;
using Settings = SadConsole.Settings;
using Sevriukoff.AsciiArena.CommonLib;
using Console = System.Console;

namespace Sevriukoff.AsciiArena.Client;

class Program
{
    private static INetworkClient _networkClient;
    
    static async Task Main(string[] args)
    {
        var builder = new ContainerBuilder();
        
        builder.RegisterType<NetworkClient>()
            .As<INetworkClient>()
            .WithParameter("serverIp", "127.0.0.1")
            .WithParameter("port", 5000)
            .SingleInstance();
        
        var container = builder.Build();

        _networkClient = container.Resolve<INetworkClient>();
        await _networkClient.ConnectAsync();
        
        _networkClient.OnGameStateReceived += (gameState) =>
        {
            //Console.WriteLine($"Player {gameState.PlayerId} at ({gameState.PlayerX}, {gameState.PlayerY})");
        };
        
        Settings.WindowTitle = "ASCII Arena Client";
        Game.Create(80, 25, GameUserInterfaceStartUp);
        Game.Instance.Run();
        Game.Instance.Dispose();

        _networkClient.Dispose();
    }

    private static void GameUserInterfaceStartUp(object? sender, GameHost e)
    {
        var loginWindow = new LoginWindow(_networkClient);
        
        loginWindow.OnLoginSuccess += (response) =>
        {
            Console.WriteLine("Login successful! Player: " + response.Player);
            // Здесь можно переключиться на основной игровой экран.
        };
        
        Game.Instance.Screen = loginWindow;
        Game.Instance.Screen.IsFocused = true;
    }
}