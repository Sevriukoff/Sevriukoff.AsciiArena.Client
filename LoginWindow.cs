using SadConsole.UI;
using SadConsole.UI.Controls;
using SadRogue.Primitives;
using Sevriukoff.AsciiArena.Client.UserInterface;
using Sevriukoff.AsciiArena.CommonLib;
using Console = SadConsole.UI.ControlsConsole;

namespace Sevriukoff.AsciiArena.Client;

public class LoginWindow : Console
{
    private TextBox _usernameTextBox;
    private TextBox _passwordTextBox;
    private Button _loginButton;
    private Button _registerButton;
    private Label _messageLabel;
    private readonly INetworkClient _networkClient;

    public event Action<LoginResponse>? OnLoginSuccess;

    public LoginWindow(INetworkClient networkClient) : base(80, 25)
    {
        _networkClient = networkClient;
        //Title = "Login / Register";
        //CanDrag = true;
        Position = new Point(0, 0);
        InitializeControls();
    }

    private void InitializeControls()
    {
        // Поле для ввода логина
        _usernameTextBox = new TextBox(30)
        {
            Position = new Point(2, 2)
        };
        Controls.Add(_usernameTextBox);

        // Поле для ввода пароля (с маскированием символов)
        _passwordTextBox = new TextBox(30)
        {
            Position = new Point(2, 4),
        };
        Controls.Add(_passwordTextBox);

        // Кнопка "Login"
        _loginButton = new Button(12)
        {
            Text = "Login",
            Position = new Point(2, 6)
        };
        _loginButton.Click += async (s, e) => await HandleLoginAsync();
        Controls.Add(_loginButton);

        // Кнопка "Register"
        _registerButton = new Button(12)
        {
            Text = "Register",
            Position = new Point(16, 6)
        };
        _registerButton.Click += async (s, e) => await HandleRegisterAsync();
        Controls.Add(_registerButton);

        // Метка для вывода сообщений
        _messageLabel = new Label("Enter credentials")
        {
            Position = new Point(2, 8),
        };
        Controls.Add(_messageLabel);
    }

    private async Task HandleLoginAsync()
    {
    }

    private async Task HandleRegisterAsync()
    {
        var username = _usernameTextBox.Text;
        var password = _passwordTextBox.Text;

        var registerCommand = new RegisterCommandMessage
        {
            Username = username,
            PasswordHash = password
        };

        await _networkClient.SendCommandAsync(registerCommand);

        _networkClient.OnGameStateReceived += state =>
        {
            var currentPlayer = state.Players.First(p => p.UserName == _usernameTextBox.Text);
            var gameWindow = new GameWindow(state.Map, currentPlayer, _networkClient);

            SadConsole.Game.Instance.Screen = gameWindow;
        };
    }

    private void OnLoginResponseReceived(LoginResponse response)
    {
    }
}