using System;
using System.Collections.Generic;
using System.Linq;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using Godot;
using static Archipelago.MultiClient.Net.Enums.HintStatus;
using static ArchipelagoMultiTextClient.Scripts.MainController;

namespace ArchipelagoMultiTextClient.Scripts;

public partial class HintTable : TextTable
{
    public static bool RefreshUI;
    public static IEnumerable<HintData> Datas = [];

    [Export] private HBoxContainer _SortBox;
    [Export] private CheckBox _ShowFound;
    [Export] private CheckBox _ShowPriority;
    [Export] private CheckBox _ShowUnspecified;
    [Export] private CheckBox _ShowNoPriority;
    [Export] private CheckBox _ShowAvoid;
    [Export] private PackedScene _DragOptionScene;
    private DraggingOptions[] _SortOptions = [];

    public static Dictionary<ItemFlags, int> ItemToSortId = new()
    {
        [ItemFlags.Advancement] = 0,
        [ItemFlags.NeverExclude] = 1,
        [ItemFlags.Trap] = 10,
    };

    public override void _Ready()
    {
        foreach (var (_, index) in MainController.Data.SortOrder.Select((s, i) => (s, i)).OrderBy(t => t.s.Index))
        {
            var drag = (DraggingOptions)_DragOptionScene.Instantiate();
            drag.DataIndex = index;
            _SortBox.AddChild(drag);
        }

        _ShowFound.Pressed += () =>
        {
            MainController.Data.HintOptions[0] = _ShowFound.ButtonPressed;
            RefreshUI = true;
        };
        _ShowPriority.Pressed += () =>
        {
            MainController.Data.HintOptions[1] = _ShowPriority.ButtonPressed;
            RefreshUI = true;
        };
        _ShowUnspecified.Pressed += () =>
        {
            MainController.Data.HintOptions[2] = _ShowUnspecified.ButtonPressed;
            RefreshUI = true;
        };
        _ShowNoPriority.Pressed += () =>
        {
            MainController.Data.HintOptions[3] = _ShowNoPriority.ButtonPressed;
            RefreshUI = true;
        };
        _ShowAvoid.Pressed += () =>
        {
            MainController.Data.HintOptions[4] = _ShowAvoid.ButtonPressed;
            RefreshUI = true;
        };
        _ShowFound.ButtonPressed = MainController.Data.HintOptions[0];
        _ShowPriority.ButtonPressed = MainController.Data.HintOptions[1];
        _ShowUnspecified.ButtonPressed = MainController.Data.HintOptions[2];
        _ShowNoPriority.ButtonPressed = MainController.Data.HintOptions[3];
        _ShowAvoid.ButtonPressed = MainController.Data.HintOptions[4];

        MetaClicked += s => DisplayServer.ClipboardSet(s.ToString());
    }

    public override void _Process(double delta)
    {
        if (!RefreshUI) return;

        var orderedHints =
            Datas.Where(hint =>
                  {
                      return hint.HintStatus switch
                      {
                          Found => _ShowFound.ButtonPressed,
                          Unspecified => _ShowUnspecified.ButtonPressed,
                          NoPriority => _ShowNoPriority.ButtonPressed,
                          Avoid => _ShowAvoid.ButtonPressed,
                          Priority => _ShowPriority.ButtonPressed,
                          _ => false
                      };
                  })
                 .OrderBy(hint => hint.LocationId);

        foreach (var option in MainController.Data.SortOrder.OrderBy(s => s.Index))
        {
            if (!option.IsActive) continue;
            switch (option.Name)
            {
                case "Receiving Player":
                    orderedHints = Order(orderedHints, hint => GetOrderSlot(hint.ReceivingPlayerSlot),
                        option.IsDescending);
                    break;
                case "Item":
                    orderedHints = Order(orderedHints, hint => SortNumber(hint.ItemFlags), option.IsDescending);
                    break;
                case "Finding Player":
                    orderedHints = Order(orderedHints, hint => GetOrderSlot(hint.FindingPlayerSlot),
                        option.IsDescending);
                    break;
                case "Priority":
                    orderedHints = Order(orderedHints, hint => HintStatusNumber[hint.HintStatus], option.IsDescending);
                    break;
            }
        }

        UpdateData(orderedHints.Select(hint => hint.GetData()).ToList());
        RefreshUI = false;
    }

    public IOrderedEnumerable<HintData> Order(IOrderedEnumerable<HintData> arr, Func<HintData, int> compare,
        bool descending)
        => !descending ? arr.OrderBy(compare) : arr.OrderByDescending(compare);

    public int GetOrderSlot(int slot) => PlayerSlots.ContainsKey(slot) ? Players.Length + slot : slot;

    public int SortNumber(ItemFlags flags) => ItemToSortId.GetValueOrDefault(flags, 2);
}

public readonly struct HintData(Hint hint)
{
    public readonly string ReceivingPlayer = Players[hint.ReceivingPlayer];
    public readonly int ReceivingPlayerSlot = hint.ReceivingPlayer;
    public readonly string Item = ItemIdToItemName(hint.ItemId, hint.ReceivingPlayer);
    public readonly ItemFlags ItemFlags = hint.ItemFlags;
    public readonly string FindingPlayer = Players[hint.FindingPlayer];
    public readonly int FindingPlayerSlot = hint.FindingPlayer;
    public readonly HintStatus HintStatus = hint.Status;
    public readonly string Location = LocationIdToLocationName(hint.LocationId, hint.FindingPlayer);
    public readonly long LocationId = hint.LocationId;
    public readonly string Entrance = hint.Entrance.Trim() == "" ? "Vanilla" : hint.Entrance;

    public string GetCopyText()
        => $"`{ReceivingPlayer}`'s __{Item}__ is in `{FindingPlayer}`'s world at **{Location}**\n-# {Entrance}";

    public string[] GetData()
    {
        var receivingPlayerColor = PlayerColor(ReceivingPlayer).Hex;
        var itemColor = MainController.Data.ColorSettings[ItemToColorId.GetValueOrDefault(ItemFlags, "item_normal")]
                                      .Hex;
        var findingPlayerColor = PlayerColor(FindingPlayer).Hex;
        var hintColor = MainController.Data.ColorSettings[HintStatusColor[HintStatus]].Hex;
        var locationColor = MainController.Data.ColorSettings["location"].Hex;
        var entranceColor = MainController.Data.ColorSettings[Entrance == "Vanilla" ? "entrance_vanilla" : "entrance"]
                                          .Hex;
        return
        [
            $"[url=\"{GetCopyText()}\"]Copy[/url]",
            $"[color={receivingPlayerColor}]{ReceivingPlayer.Clean()}[/color]",
            $"[color={itemColor}]{Item.Clean()}[/color]",
            $"[color={findingPlayerColor}]{FindingPlayer.Clean()}[/color]",
            $"[color={hintColor}]{HintStatusText[HintStatus]}[/color]",
            $"[color={locationColor}]{Location.Clean()}[/color]",
            $"[color={entranceColor}]{Entrance.Clean()}[/color]"
        ];
    }
}