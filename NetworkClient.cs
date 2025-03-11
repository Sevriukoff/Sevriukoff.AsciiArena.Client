using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Sevriukoff.AsciiArena.CommonLib;

namespace Sevriukoff.AsciiArena.Client;

public class NetworkClient(string serverIp, int port) : INetworkClient, IDisposable
{
    private TcpClient? _client;
    private NetworkStream? _stream;

    public event Action<GameState?>? OnGameStateReceived;
    
    public async Task ConnectAsync()
    {
        _client = new TcpClient();
        await _client.ConnectAsync(serverIp, port);
        
        _stream = _client.GetStream();
        Console.WriteLine("Connected to server at " + serverIp + ":" + port);
        
        _ = Task.Run(ReceiveMessagesAsync);
    }

    public async Task SendCommandAsync(CommandMessage command)
    {
        var json = JsonSerializer.Serialize(command);
        var data = Encoding.UTF8.GetBytes(json);

        if (_stream != null)
            await _stream.WriteAsync(data);

        Console.WriteLine("Sent command: " + json);
    }

    public void Dispose()
    {
        _stream?.Dispose();
        _client?.Close();
    }
    
    private async Task ReceiveMessagesAsync()
    {
        var buffer = new byte[4096];
        
        try
        {
            while (_client.Connected)
            {
                var bytesRead = await _stream.ReadAsync(buffer);
                
                if (bytesRead == 0)
                {
                    break;
                }
                string json = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Console.WriteLine("Received JSON: " + json);

                try
                {
                    var gameState = JsonSerializer.Deserialize<GameState>(json);
                    OnGameStateReceived?.Invoke(gameState);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("JSON deserialization error: " + ex.Message);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error receiving messages: " + ex.Message);
        }
    }
}