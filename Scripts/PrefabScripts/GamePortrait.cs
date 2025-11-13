using Godot;
using System;
using ArchipelagoMultiTextClient.Scripts.HintTab;
using ArchipelagoMultiTextClient.Scripts.LoginTab;
using CreepyUtil.Archipelago.ApClient;
using static ArchipelagoMultiTextClient.Scripts.MainController;

public partial class GamePortrait : Control
{
    [Export] private Vector2 _ImageSize = new(300, 450);
    [Export] private TextureRect _Image;
    [Export] private Label _SlotName;
    [Export] private Label _CheckCount;
    [Export] private Label _ErrorText;
    [Export] private ColorRect _ConnectionTint;
    [Export] private ColorRect _MainTint;
    [Export] private Color _HoverTint;
    [Export] private Color _IdleTint;
    [Export] private Color _ConnectingTint;
    [Export] private Color _ConnectedTint;
    [Export] private Color _ErrorTint;
    private double _Timer;
    private double _ConnectionTimer;
    private bool _IsConnecting;
    private ConnectionStatus _Status = ConnectionStatus.NotConnected;

    public event Action? OnTileLeftClicked;
    public event Action? OnTileRightClicked;

    private GameClient _Client = new();
    public ConnectionStatus Status => _Status;

    public string SlotName
    {
        get => _SlotName.Text;
        set => _SlotName.Text = value;
    }

    public override void _Ready()
    {
        SetErrorText("");
        UpdateCheckCount(0, 0);

        _Client.CheckedLocationsUpdated += _ =>
        {
            var cache = MultiworldName.CurrentWorld.LocationCheckCountCaches[_Client.PlayerName] =
                new LocationCheckCountCache(_Client.LocationsCheckedCount, _Client.LocationCount);
            UpdateCheckCount(cache.AmountChecked, cache.CheckCount);
        };

        _Client.ConnectionStatusChanged += status => { CallDeferred("SetStatus", (int)status); };

        OnTileLeftClicked += () =>
        {
            if (Status is ConnectionStatus.Connecting) return;
            if (Status is ConnectionStatus.NotConnected or ConnectionStatus.Error)
                _Client.TryConnection(Main.Port, SlotName, Main.Address, Main.Password);
            if (Status is ConnectionStatus.Connected) _Client.TryDisconnection();
        };
    }

    public override void _Process(double delta)
    {
        if (_Status is ConnectionStatus.Connecting)
        {
            _ConnectionTimer += delta;
            _ConnectionTint.Color = _IdleTint.Lerp(_ConnectingTint, (float)WaveWeight(_ConnectionTimer, 5));
        }

        if (_Image.GetGlobalRect().HasPoint(GetGlobalMousePosition()))
        {
            _Timer += delta * 3;
        }
        else _Timer -= delta * 1.3f;

        _Timer = Math.Clamp(_Timer, 0, 1);
        _MainTint.Color = _IdleTint.Lerp(_HoverTint, (float)_Timer);
        _Client.Update(delta);
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is not InputEventMouseButton button) return;
        if (!button.Pressed) return;
        if (button.ButtonIndex is MouseButton.Left)
        {
            OnTileLeftClicked?.Invoke();
        }
        else if (button.ButtonIndex is MouseButton.Right)
        {
            OnTileRightClicked?.Invoke();
        }
    }

    public void SetImageScale(float scale)
    {
        CustomMinimumSize = _ImageSize * scale;
        _Image.SetScale(new Vector2(scale, scale));
    }

    public void SetStatus(int status) => SetStatus((ConnectionStatus)status);

    public void SetStatus(ConnectionStatus status)
    {
        _ConnectionTimer = 0;
        _Status = status;
        SetConnectionColor();
    }

    private void SetConnectionColor()
    {
        switch (_Status)
        {
            case ConnectionStatus.Error:
                _ConnectionTint.Color = _ErrorTint;
                SetErrorText(_Client.Error is null ? "" : $"ERROR:\n{string.Join("\n", _Client.Error)}");
                break;
            case ConnectionStatus.Connected:
                _ConnectionTint.Color = _ConnectedTint;
                break;
            default:
                _ConnectionTint.Color = Colors.Transparent;
                break;
        }
    }

    public void SetErrorText(string text)
    {
        _ErrorText.Visible = text != "";
        _ErrorText.Text = text;
    }

    public double WaveWeight(double x, double scale)
    {
        var correction = 1.6f / scale;
        var sin = Mathf.Sin((x - correction) * scale);
        return Math.Min(sin + 1, 1);
    }

    public void UpdateFromGameData(GameData data)
    {
        if (SlotName != data.SlotName)
        {
            SlotName = data.SlotName;
        }

        if (data.GameName is null)
        {
            _Image.Texture = Main.UnknownGamePortrait;
        }
        else if (GamePortraits.TryGetValue(data.GameName, out var portrait))
        {
            _Image.Texture = portrait;
        }
    }

    public void UpdateCheckCount(int amount, int max)
    {
        var parent = (Control)_CheckCount.GetParent();
        parent.CallDeferred("set_visible", max != 0);
        if (max == 0) return;
        _CheckCount.CallDeferred("set_text", $"{amount:###,##0}/{max:###,##0}");
    }
}

public enum ConnectionStatus
{
    NotConnected,
    Connecting,
    Connected,
    Error
}