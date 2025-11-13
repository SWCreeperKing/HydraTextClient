using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using ArchipelagoMultiTextClient.Scripts.HintTab;
using ArchipelagoMultiTextClient.Scripts.LoginTab;

public partial class SlotView : HFlowContainer
{
    [Export] private PackedScene _GamePortraitScene;
    [Export] private SpinBox _ScaleBox;
    private Dictionary<string, GamePortrait> _Portraits = [];
    // private Dictionary<string, SlotClient> _Clients = [];
    private List<string> NamesOrder = [];
    private string[] _MultiworldSlots;

    // public Dictionary<string, SlotClient>.ValueCollection Clients => _Clients.Values;

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

    public GamePortrait AddClient(GameData data)
    {
        var portrait = _GamePortraitScene.Instantiate<GamePortrait>();
        portrait.UpdateFromGameData(data);
        portrait.SetImageScale((float)_ScaleBox.Value);

        NamesOrder.Add(data.SlotName);
        AddChild(portrait);
        _Portraits[data.SlotName] = portrait;
        ReorderChildren();
        return portrait;
    }

    public void RemoveClient(string playerName)
    {
        var portrait = _Portraits[playerName];
        NamesOrder.Remove(playerName);
        RemoveChild(portrait);
        _Portraits.Remove(playerName);
        ReorderChildren();
    }

    public void ReorderChildren()
    {
        if (NamesOrder.Count == 1) return;
        NamesOrder = NamesOrder.OrderByDescending(s =>
                                {
                                    if (_MultiworldSlots is null || _MultiworldSlots.Length == 0) return false;
                                    return _MultiworldSlots.Contains(s);
                                })
                               .ThenBy(s => s)
                               .ToList();

        for (var i = 0; i < NamesOrder.Count; i++)
        {
            MoveChild(_Portraits[NamesOrder[i]], i);
        }
    }

    public void MatchWorldSlots(params string[] slots)
    {
        _MultiworldSlots = slots;
        ReorderChildren();
    }

    public void ResetCounts()
    {
        foreach (var portrait in _Portraits.Values)
        {
            portrait.UpdateCheckCount(0, 0);
        }
    }

    public void UpdateCounts()
    {
        if (MultiworldName.CurrentWorld is null) return;
        foreach (var (slot, cache) in MultiworldName.CurrentWorld.LocationCheckCountCaches)
        {
            if (!_Portraits.TryGetValue(slot, out var value)) return;
            value.UpdateCheckCount(cache.AmountChecked, cache.CheckCount);
        }
    }

    public bool AnyRunning()
        => _Portraits.Any(kv => kv.Value.Status is ConnectionStatus.Connecting or ConnectionStatus.Connected);
}