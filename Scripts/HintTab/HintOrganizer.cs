using System;
using System.Collections.Generic;
using System.Linq;
using ArchipelagoMultiTextClient.Scripts.PrefabScripts;
using Godot;
using static ArchipelagoMultiTextClient.Scripts.LoginTab.MultiworldName;
using static ArchipelagoMultiTextClient.Scripts.MainController;
using static ArchipelagoMultiTextClient.Scripts.SettingsTab.Settings;

namespace ArchipelagoMultiTextClient.Scripts.HintTab;

public partial class HintOrganizer : HSplitContainer
{
    public static bool RefreshUI;
    [Export] private HFlowContainer _InventoryContainer;
    [Export] private DragAndDropData _ListContainer;
    [Export] private DragAndDropData _IndexFollower;
    [Export] private PackedScene _PanelTextScene;
    private Dictionary<string, PanelText> _HintTiles = [];

    public override void _EnterTree()
        => OnMultiworldChanged += mw =>
        {
            Clear();
            if (mw is null) return;
            Load(mw.ItemOrder);
        };

    public override void _Ready()
    {
        Action<string> dropAction = s =>
        {
            var panel = _HintTiles[s];

            if (panel.IsSquarePanel)
            {
                _InventoryContainer.RemoveChild(panel);
                _ListContainer.AddChild(panel);
                panel.IsSquarePanel = false;
            }

            var index = _ListContainer.GetChildren().IndexOf(_IndexFollower);
            _ListContainer.MoveChild(panel, index);
            CurrentWorld.ItemOrder = _ListContainer.GetChildren()
                                                   .Where(child => child is PanelText)
                                                   .Select(child => ((PanelText)child).Id)
                                                   .ToArray();
            CurrentWorld.Changed = true;
        };

        _ListContainer.OnDropData += s => dropAction(s);
        _IndexFollower.OnDropData += s => dropAction(s);
    }

    public override void _Process(double delta)
    {
        if (RefreshUI) RefreshUi();
    }

    public void RefreshUi()
    {
        RefreshUI = false;

        foreach (var (id, data) in CurrentWorld.HintDatas)
        {
            if (_HintTiles.ContainsKey(id))
            {
                // if (data.HintStatus is not HintStatus.Priority) RemoveHintTile(id);
                continue;
            }

            if (!IsPlayerSlotALoginSlot(data.FindingPlayerSlot)) continue;
            // if (data.HintStatus is not HintStatus.Priority) continue;
            AddHintTile(data);
        }
    }

    public void AddHintTile(HintData data, bool addToInventory = true)
    {
        var flowText = _PanelTextScene.Instantiate<PanelText>();
        GenerateNames(data, out flowText.NormalText, out flowText.SquareText);
        flowText.SetScript("HintTab/HintDragable");
        flowText.IsSquarePanel = addToInventory;
        flowText.Id = data.Id;
        if (addToInventory) _InventoryContainer.AddChild(flowText);
        else _ListContainer.AddChild(flowText);
        _HintTiles.Add(data.Id, flowText);
    }

    public void RemoveHintTile(string id)
    {
        var tile = _HintTiles[id];
        _HintTiles.Remove(id);
        tile.GetParent().RemoveChild(tile);
        tile.QueueFree();
    }

    public void GenerateNames(HintData data, out string normalName, out string squareName)
    {
        var receivingPlayerColor = PlayerColor(data.ReceivingPlayerSlot).Hex;
        var metaString = ItemFilterDialog.GetMetaString(data.Item, PlayerGames[data.ReceivingPlayerSlot], data.ItemId,
            data.ItemFlags);
        var itemColor = GetItemHexColor(data.ItemFlags, metaString);
        var itemBgColor = GetItemHexBgColor(data.ItemFlags, metaString);
        var locationColor = Data["location"].Hex;
        var entranceColor = Data[data.Entrance == "Vanilla" ? "entrance_vanilla" : "entrance"].Hex;

        squareName = $"""
                      [color={receivingPlayerColor}]{data.ReceivingPlayer}[/color]'s

                      [bgcolor={itemBgColor}][color={itemColor}]{data.Item}[/color][/bgcolor]
                      """;

        normalName =
            $"""
             [color={receivingPlayerColor}]{data.ReceivingPlayer}[/color]'s [bgcolor={itemBgColor}][color={itemColor}]{data.Item}[/color][/bgcolor] is at [color={locationColor}]{data.Location}[/color]
               - [color={entranceColor}]{data.Entrance}[/color]
             """;
    }

    public void Load(string[] array)
    {
        foreach (var (id, hint) in CurrentWorld.HintDatas)
        {
            AddHintTile(hint, !array.Contains(id));
        }
    }

    public void Clear()
    {
        foreach (var child in _InventoryContainer.GetChildren())
        {
            _InventoryContainer.RemoveChild(child);
            child.QueueFree();
        }

        foreach (var child in _ListContainer.GetChildren().Where(child => child is PanelText))
        {
            _ListContainer.RemoveChild(child);
            child.QueueFree();
        }
    }
}