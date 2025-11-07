using Godot;
using System;

public partial class GamePortrait : Control
{
    [Export] private TextureRect _Image;
    [Export] private Label _SlotName;
    [Export] private Label _CheckCount;
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
    private string[]? _ErrorText = null;
    private ConnectionStatus _Status = ConnectionStatus.NotConnected;

    public event Action? OnTileClicked;

    public override void _Ready()
    {
        // for testing
        OnTileClicked += () =>
        {
            if (_Status is not ConnectionStatus.Connecting)
            {
                SetStatus(ConnectionStatus.Connecting);
            }
            else
            {
                SetStatus(ConnectionStatus.NotConnected);
            }
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
            _Timer += delta * 2;
        }
        else _Timer -= delta * 1.3f;

        _Timer = Math.Clamp(_Timer, 0, 1);
        _MainTint.Color = _IdleTint.Lerp(_HoverTint, (float)_Timer);
    }

    public void SetStatus(ConnectionStatus status, string[]? error = null)
    {
        _ConnectionTimer = 0;
        _ErrorText = error;
        _Status = status;
        switch (_Status)
        {
            case ConnectionStatus.Error:
                _ConnectionTint.Color = _ErrorTint;
                break;
            case ConnectionStatus.Connected:
                _ConnectionTint.Color = _ConnectedTint;
                break;
            default:
                _ConnectionTint.Color = Colors.Transparent;
                break;
        }
    }

    public double WaveWeight(double x, double scale)
    {
        var correction = 1.6f / scale;
        var sin = Mathf.Sin((x - correction) * scale);
        return Math.Min(sin + 1, 1);
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is not InputEventMouseButton button) return;
        if (!button.Pressed) return;
        if (button.ButtonIndex is MouseButton.Left)
        {
            OnTileClicked?.Invoke();
        }
        else if (button.ButtonIndex is MouseButton.Right) // only for testing
        {
            if (_Status is ConnectionStatus.Connecting)
            {
                SetStatus(ConnectionStatus.Connected);
            }
            else
            {
                SetStatus(ConnectionStatus.Error, ["Test Error"]);
            }
        }
    }
}

public enum ConnectionStatus
{
    NotConnected,
    Connecting,
    Connected,
    Error
}