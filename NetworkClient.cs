using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Sevriukoff.AsciiArena.CommonLib;

namespace Sevriukoff.AsciiArena.Client;

public class NetworkClient(string serverIp, int port) : INetworkClient
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
        if (_stream == null)
            return;
        
        var json = JsonSerializer.Serialize(command);
        var data = Encoding.UTF8.GetBytes(json);
        
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
        if (_client == null || _stream == null)
            return;
        
        var buffer = new byte[8192 * 2];
        
        try
        {
            while (_client.Connected)
            {
                var bytesRead = await _stream.ReadAsync(buffer);
                
                if (bytesRead == 0)
                    break;
                
                var json = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Console.WriteLine("Received JSON: " + json);

                try
                {
                    var loginResponse = JsonSerializer.Deserialize<LoginResponse>(json);
                    
                    var gameState = new GameState
                    {
                        // Преобразуем MapDto в Map
                        Map = ConvertMapDtoToMap(To2DArray(loginResponse.MapData)),
                        // Для примера создаем список игроков с одним элементом, преобразованным из PlayerDto.
                        // Можно написать метод ConvertPlayerDtoToPlayer или использовать AutoMapper.
                        Players = new List<Player>
                        {
                            new Player(loginResponse.Player.Id, loginResponse.Player.UserName, 100, loginResponse.Player.X, loginResponse.Player.Y)
                        },
                        
                        StartX = loginResponse.StartX,
                        StartY = loginResponse.StartY
                    };
                    
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
    
    public static T[,] To2DArray<T>(T[][] jaggedArray)
    {
        int rows = jaggedArray.Length;
        int cols = jaggedArray[0].Length;

        var result = new T[rows, cols];

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                result[i, j] = jaggedArray[i][j];
            }
        }

        return result;
    }

    
    public static Map ConvertMapDtoToMap(Cell[,] dto)
    {
        var map = new Map(20, 20);
        for (int y = 0; y < 20; y++)
        {
            for (int x = 0; x < 20; x++)
            {
                map.Cells[x, y] = dto[x, y].Type;
            }
        }
        return map;
    }
}