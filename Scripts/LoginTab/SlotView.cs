using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using ArchipelagoMultiTextClient.Scripts.LoginTab;

public partial class SlotView : HFlowContainer
{
    [Export] private PackedScene _GamePortraitScene;
    [Export] private SpinBox _ScaleBox;
    private Dictionary<string, GamePortrait> _Portraits = [];
    private Dictionary<string, SlotClient> _Clients = [];
    private List<string> NamesOrder = [];

    public Dictionary<string, SlotClient>.ValueCollection Clients => _Clients.Values;

    public override void _EnterTree()
    {
        _ScaleBox.ValueChanged += d =>
        {
            foreach (var (_, portrait) in _Portraits)
            {
                portrait.SetImageScale((float)d);
            }
        };
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is not InputEventMouseButton button) return;
        if (!Input.IsKeyPressed(Key.Ctrl)) return;
        if (button.ButtonIndex is MouseButton.WheelUp) _ScaleBox.SetValue(_ScaleBox.Value + _ScaleBox.Step);
        if (button.ButtonIndex is MouseButton.WheelDown) _ScaleBox.SetValue(_ScaleBox.Value - _ScaleBox.Step);
    }

    public bool HasSlotName(string name) => _Portraits.Keys.Any(client => client == name);

    public void AddClient(string playerName)
    {
        var portrait = _GamePortraitScene.Instantiate<GamePortrait>();
        portrait.Client = new SlotClient();
        portrait.SlotName = playerName;
        portrait.SetImageScale((float)_ScaleBox.Value);

        portrait.Client.ConnectionStatusChanged += status => portrait.CallDeferred("SetStatus", (int)status);

        portrait.OnTileLeftClicked += () =>
        {
            if (portrait.Status is ConnectionStatus.Connecting) return;
            if (portrait.Status is ConnectionStatus.NotConnected or ConnectionStatus.Error)
                portrait.Client.TryConnection();
            if (portrait.Status is ConnectionStatus.Connected) portrait.Client.TryDisconnection();
        };

        NamesOrder.Add(playerName);
        AddChild(portrait);
        _Portraits[playerName] = portrait;
        _Clients[playerName] = portrait.Client;
        ReorderChildren();
    }

    public void RemoveClient(string playerName)
    {
        var portrait = _Portraits[playerName];
        NamesOrder.Remove(playerName);
        RemoveChild(portrait);
        _Portraits.Remove(playerName);
        _Clients.Remove(playerName);
        ReorderChildren();
    }

    public void ReorderChildren()
    {
        if (NamesOrder.Count == 1) return;
        NamesOrder = NamesOrder.Order().ToList();

        for (var i = 0; i < NamesOrder.Count; i++)
        {
            MoveChild(_Portraits[NamesOrder[i]], i);
        }
    }
}