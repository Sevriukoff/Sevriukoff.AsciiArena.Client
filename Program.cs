using System.Net.Sockets;
using System.Text;

namespace Sevriukoff.AsciiArena.Client;

class Program
{
    private const string ServerIp = "127.0.0.1";
    private const int Port = 8080;
    
    static async Task Main(string[] args)
    {
        try
        {
            var client = new TcpClient();
            await client.ConnectAsync(ServerIp, Port);
            Console.WriteLine("Подключено к серверу.");

            var stream = client.GetStream();

            var message = "Hello, server!";
            var data = Encoding.UTF8.GetBytes(message);
            await stream.WriteAsync(data);
            Console.WriteLine($"Отправлено: {message}");

            var buffer = new byte[1024];
            
            while (true)
            {
                var bytesRead = await stream.ReadAsync(buffer);
                
                if (bytesRead == 0)
                    break;
                
                var response = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Console.WriteLine("Received: " + response);
            }

            client.Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Ошибка: " + ex.Message);
        }
    }
}