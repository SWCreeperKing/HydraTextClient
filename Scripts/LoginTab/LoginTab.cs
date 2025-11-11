using Godot;
using System;
using System.Linq;
using ArchipelagoMultiTextClient.Scripts;
using ArchipelagoMultiTextClient.Scripts.LoginTab;
using CreepyUtil.DiscordRpc;
using static ArchipelagoMultiTextClient.Scripts.MainController;

public partial class LoginTab : ScrollContainer
{
    [Export] private MainController _MainController;
    [Export] private LineEdit _AddressField;
    [Export] private LineEdit _PasswordField;
    [Export] private LineEdit _PortField;
    [Export] private Button _SlotAddButton;
    [Export] private Label _ConnectionTimer;
    [Export] private Timer _DiscordTimer;
    [Export] private Label _DiscordText;
    [Export] private Button _DiscordReconnect;
    [Export] private Label _VersionLabel;
    [Export] private Button _SaveButton;
    [Export] private MultiworldName _NameManager;
    [Export] private SlotView _SlotView;

    public override void _EnterTree() { _VersionLabel.Text += _MainController.Version; }

    public override void _Ready()
    {
        _DiscordTimer.Start();
        _NameManager.ChangeState(MultiworldState.None);
        // _SlotAddButton.Pressed += () => TryAddSlot(Slot); todo: override original
        _AddressField.TextChanged += s => Data.Address = s;
        _PasswordField.TextChanged += s => Data.Password = s;
        _AddressField.Text = Data.Address;
        _PasswordField.Text = Data.Password;
        _PortField.Text = $"{Data.Port}";
        _SaveButton.Text = "Safty Save";

        foreach (var player in Data.SlotNames)
        {
            AddSlot(player);
        }
    }

    public override void _Process(double delta)
    {
        _DiscordText.Visible = DiscordIntegration.DiscordAlive;
        _DiscordReconnect.Visible = !DiscordIntegration.DiscordAlive;

        // ReSharper disable once AssignmentInConditionalExpression
        if (_ConnectionTimer.Visible = ConnectionCooldown > 0) // intentional (because funny)
        {
            _ConnectionTimer.Text = $"Connection Cooldown: {ConnectionCooldown:0.00}s (as to not spam the server)";
            ConnectionCooldown -= delta;
        }

        var anyRunning = ClientList.Values.Any(client => client.IsRunning is not null && client.IsRunning!.Value);
        ToggleLockInput(!anyRunning);
    }

    public void TryAddSlot(string slot)
    {
        if (slot.Trim() == "" || ClientList.ContainsKey(slot.Trim())) return;
        AddSlot(slot.Trim());
        Data.SlotNames.Add(slot.Trim());
    }

    public void AddSlot(string playerName)
    {
        var client = new SlotClient();
        client.PlayerName = playerName;
        client.Main = _MainController;
        ClientList.Add(playerName, client);
        _SlotView.AddClient(client);
    }

    public void RemoveSlot(string playerName)
    {
        var client = ClientList[playerName];
        _SlotView.RemoveClient(client);
        ClientList.Remove(playerName);
        Data.SlotNames.Remove(playerName);
        client.QueueFree();
    }

    public void ToggleLockInput(bool toggle)
    {
        _AddressField.Editable = toggle;
        _PasswordField.Editable = toggle;
        _PortField.Editable = toggle;
    }

    public void ChangeMultiworldState(MultiworldState state) => _NameManager.ChangeState(state);
}