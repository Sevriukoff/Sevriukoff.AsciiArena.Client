using SadConsole;
using SadConsole.Input;
using SadRogue.Primitives;
using Sevriukoff.AsciiArena.CommonLib;
using Console = SadConsole.Console;

namespace Sevriukoff.AsciiArena.Client.UserInterface;

public class GameWindow : Console
{
    private readonly Map _map;
    private readonly Player _currentPlayer;
    private const int LoadThreshold = 1;
    private readonly INetworkClient _networkClient;

    public GameWindow(Map map, Player currentPlayer, INetworkClient networkClient) : base(map.Width, map.Height)
    {
        _map = map;
        _currentPlayer = currentPlayer;
        _networkClient = networkClient;
        RenderMap();

        SadConsole.Game.Instance.FrameUpdate += Update;
    }

    private void RenderMap()
    {
        for (int x = 0; x < _map.Width; x++)
        {
            for (int y = 0; y < _map.Height; y++)
            {
                CellType cellType = _map.Cells[x, y];

                char glyph = cellType switch
                {
                    CellType.Floor => '.',
                    CellType.Wall  => '#',
                    CellType.Tree  => '^',
                    _              => '?'
                };
                
                Color color = cellType switch
                {
                    CellType.Floor => Color.Gray,
                    CellType.Wall  => Color.White,
                    CellType.Tree  => Color.Green,
                    _              => Color.Red
                };

                this.SetGlyph(x, y, glyph, color);
            }
        }

        this.SetGlyph(_currentPlayer.X, _currentPlayer.Y, '@', Color.Purple);
    }

    private void Update(object sender, GameHost e)
    {
        bool hasMoved = false;

        if (SadConsole.GameHost.Instance.Keyboard.IsKeyPressed(Keys.Up))
            hasMoved = TryMove(0, -1);
        else if (SadConsole.GameHost.Instance.Keyboard.IsKeyPressed(Keys.Down))
            hasMoved = TryMove(0, 1);
        else if (SadConsole.GameHost.Instance.Keyboard.IsKeyPressed(Keys.Left))
            hasMoved = TryMove(-1, 0);
        else if (SadConsole.GameHost.Instance.Keyboard.IsKeyPressed(Keys.Right))
            hasMoved = TryMove(1, 0);

        if (hasMoved)
        {
            RenderMap();
        }
    }

    private bool TryMove(int dx, int dy)
    {
        int newX = _currentPlayer.X + dx;
        int newY = _currentPlayer.Y + dy;

        if (newX < 0 || newX >= _map.Width || newY < 0 || newY >= _map.Height)
            return false;

        if (_map.Cells[newX, newY] == CellType.Floor)
        {
            _currentPlayer.X = newX;
            _currentPlayer.Y = newY;

            CheckForMapLoadTrigger(newX, newY);

            return true;
        }

        return false;
    }
    
    private void CheckForMapLoadTrigger(int playerX, int playerY)
    {
        bool nearLeftEdge = playerX <= LoadThreshold;
        bool nearRightEdge = playerX >= _map.Width - LoadThreshold - 1;
        bool nearTopEdge = playerY <= LoadThreshold;
        bool nearBottomEdge = playerY >= _map.Height - LoadThreshold - 1;

        if (nearLeftEdge || nearRightEdge || nearTopEdge || nearBottomEdge)
        {
            // Отправка запроса на сервер для подгрузки следующей части карты
            RequestMapChunkFromServer(playerX, playerY);
        }
    }

    private async void RequestMapChunkFromServer(int playerX, int playerY)
    {
        var chunkRequest = new ChunkCommandMessage()
        {
            PlayerId = _currentPlayer.Id,
            PlayerX = playerX,
            PlayerY = playerY
        };

        await _networkClient.SendCommandAsync(chunkRequest);

        _networkClient.OnGameStateReceived += chunk =>
        {
            MergeMapChunk(chunk);
            RenderMap();
        };
    }
    
    private void MergeMapChunk(GameState chunk)
    {
        int chunkWidth = chunk.Map.Cells.GetLength(0);
        int chunkHeight = chunk.Map.Cells.GetLength(1);

        for (int y = 0; y < chunkHeight; y++)
        {
            for (int x = 0; x < chunkWidth; x++)
            {
                int globalX = chunk.StartX + x;
                int globalY = chunk.StartY + y;

                if (globalX >= 0 && globalX < _map.Width && globalY >= 0 && globalY < _map.Height)
                {
                    _map.Cells[globalX, globalY] = chunk.Map.Cells[x, y];
                }
            }
        }
    }
}