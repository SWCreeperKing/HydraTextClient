using System;
using System.Collections.Generic;
using System.Linq;
using Archipelago.MultiClient.Net.Enums;
using ArchipelagoMultiTextClient.Scripts.PrefabScripts;
using Godot;
using static ArchipelagoMultiTextClient.Scripts.LoginTab.MultiworldName;
using static ArchipelagoMultiTextClient.Scripts.MainController;

namespace ArchipelagoMultiTextClient.Scripts.HintTab;

public partial class HintOrganizer : HSplitContainer
{
    public static bool RefreshUI;
    [Export] private HFlowContainer _InventoryContainer;
    [Export] private PackedScene _PanelTextScene;
    [Export] private DragAndDropDataFolder _Folder;
    [Export] private PackedScene TEMPFOLDER;
    private Dictionary<string, PanelText> _HintTiles = [];

    public override void _EnterTree()
        => OnMultiworldChanged += mw =>
        {
            Clear();
            _Folder.Title = mw is null ? "No Multiworld Selected" : mw.Name;
            if (mw is null) return;
            Load(mw.ItemOrder);
        };

    public override void _Ready()
    {
        Action<DragAndDropDataFolder, string> dropAction = (folder, id) =>
        {
            var panel = _HintTiles[id];

            if (panel.IsSquarePanel) panel.IsSquarePanel = false;
            folder.MoveChildToFollower(panel);
            
            CurrentWorld.ItemOrder = folder.GetTextIdsInChildren();
            CurrentWorld.Changed = true;
        };

        _Folder.SetOnDropData(dropAction);
        _Folder.IsDragging = () => HintDragable.IsDragging;
    }

    public override void _Process(double delta)
    {
        if (!RefreshUI) return;

        foreach (var (id, data) in CurrentWorld.HintDatas)
        {
            if (_HintTiles.ContainsKey(id))
            {
                if (data.HintStatus is not HintStatus.Priority)
                {
                    RemoveHintTile(id);
                    CurrentWorld.Changed = true;
                }
                continue;
            }

            if (!IsPlayerSlotALoginSlot(data.FindingPlayerSlot)) continue;
            if (data.HintStatus is not HintStatus.Priority) continue;
            AddHintTile(data);
        }
        
        RefreshUI = false;
    }

    public void AddHintTile(HintData data, bool addToInventory = true)
    {
        if (_HintTiles.TryGetValue(data.Id, out var original))
        {
            GenerateNames(data, out original.NormalText, out original.SquareText);
            if (original.IsSquarePanel) _InventoryContainer.RemoveChild(original);
            original.IsSquarePanel = addToInventory;
            
            if (addToInventory) _InventoryContainer.AddChild(original);
            else _Folder.AddChildToList(original);
            return;
        }
        
        var flowText = _PanelTextScene.Instantiate<PanelText>();
        GenerateNames(data, out flowText.NormalText, out flowText.SquareText);
        flowText.SetScript("HintTab/HintDragable");
        flowText.IsSquarePanel = addToInventory;
        flowText.Id = data.Id;
        if (addToInventory) _InventoryContainer.AddChild(flowText);
        else _Folder.AddChildToList(flowText);
        _HintTiles[data.Id] = flowText;
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
        var item = FormatItemColor(data.Item, PlayerGames[data.ReceivingPlayerSlot], data.ItemId, data.ItemFlags,
            false);
        var locationColor = Data["location"].Hex;
        var entranceColor = Data[data.Entrance == "Vanilla" ? "entrance_vanilla" : "entrance"].Hex;

        squareName = $"""
                      [color={receivingPlayerColor}]{data.ReceivingPlayer}[/color]'s

                      {item}
                      """;

        normalName =
            $"""
             [color={receivingPlayerColor}]{data.ReceivingPlayer}[/color]'s {item} is at [color={locationColor}]{data.Location}[/color]
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

        _Folder.ClearFolder();
    }
}