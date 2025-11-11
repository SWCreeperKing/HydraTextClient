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
    private List<string> NamesOrder = [];

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

    public void AddClient(SlotClient client)
    {
        var name = client.PlayerName;
        var portrait = _GamePortraitScene.Instantiate<GamePortrait>();
        portrait.SlotName = name;
        portrait.SetImageScale((float)_ScaleBox.Value);

        client.ConnectionStatusChanged += (status, error) =>
        {
            switch (status)
            {
                case ConnectionStatus.NotConnected:
                    portrait.SetStatus(ConnectionStatus.NotConnected, error);
                    break;
                case ConnectionStatus.Connecting:
                    portrait.SetStatus(ConnectionStatus.Connecting, error);
                    break;
                case ConnectionStatus.Connected:
                    portrait.SetStatus(ConnectionStatus.Connected, error);
                    break;
                case ConnectionStatus.Error:
                    portrait.SetStatus(ConnectionStatus.Error, error);
                    break;
            }
        };

        portrait.OnTileLeftClicked += () =>
        {
            if (portrait.Status is ConnectionStatus.Connecting) return;
            if (portrait.Status is ConnectionStatus.NotConnected or ConnectionStatus.Error) client.TryConnection();
            if (portrait.Status is ConnectionStatus.Connected) client.TryDisconnection();
        };

        NamesOrder.Add(name);
        AddChild(portrait);
        GetParent().AddChild(client);
        _Portraits[name] = portrait;
        ReorderChildren();
    }

    public void RemoveClient(SlotClient client)
    {
        var name = client.PlayerName;
        var portrait = _Portraits[name];
        NamesOrder.Remove(name);
        RemoveChild(portrait);
        GetParent().AddChild(client);
        _Portraits.Remove(name);
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