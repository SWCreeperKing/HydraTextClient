using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using ArchipelagoMultiTextClient.Scripts;
using ArchipelagoMultiTextClient.Scripts.Extra;
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
    [Export] private GameConfigWindow _ConfigWindow;

    // public Dictionary<string,SlotClient>.ValueCollection Clients => _SlotView.Clients; 
    
    public override void _EnterTree() { _VersionLabel.Text += _MainController.Version; }

    public override void _Ready()
    {
        _DiscordTimer.Start();
        _NameManager.ChangeState(MultiworldState.None);
        _SlotAddButton.Pressed += () => _ConfigWindow.ShowConfig(this);
        _AddressField.TextChanged += s => Data.Address = s;
        _PasswordField.TextChanged += s => Data.Password = s;
        _AddressField.Text = Data.Address;
        _PasswordField.Text = Data.Password;
        _PortField.Text = $"{Data.Port}";
        _SaveButton.Text = "Safty Save";
        _SaveButton.Pressed += Save;
        _DiscordReconnect.Pressed += TryReconnectDiscord;

        foreach (var (name, data) in Data.GameData)
        {
            AddSlot(data);
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

        ToggleLockInput(!_SlotView.AnyRunning());
    }

    public bool HasSlotName(string slotName) => _SlotView.HasSlotName(slotName);
    
    public void TryAddSlot(GameData data)
    {
        var slotName = data.SlotName = data.SlotName.Trim();
        if (slotName == "" || HasSlotName(slotName)) return;
        AddSlot(data);
        // Data.SlotNames.Add(slot.Trim()); // todo: add slot
    }
    
    public void AddSlot(GameData data)
    {
        var portrait = _SlotView.AddClient(data);
        portrait.OnTileRightClicked += () =>
        {
            _ConfigWindow.ShowConfig(this, portrait);
        };
    }

    public void RemoveSlot(string playerName)
    {
        _SlotView.RemoveClient(playerName);
        // Data.SlotNames.Remove(playerName); todo: remove slot
    }

    public void ToggleLockInput(bool toggle)
    {
        _AddressField.Editable = toggle;
        _PasswordField.Editable = toggle;
        _PortField.Editable = toggle;
    }

    public void ChangeMultiworldState(MultiworldState state) => _NameManager.ChangeState(state);
    public void MatchWorldSlots(params string[] slots) => _SlotView.MatchWorldSlots(slots);

    public void ResetCounts() => _SlotView.ResetCounts();
    public void UpdateCounts() => _SlotView.UpdateCounts();
}