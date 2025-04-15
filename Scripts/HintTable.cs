using System;
using System.Collections.Generic;
using System.Linq;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using Godot;
using static Archipelago.MultiClient.Net.Enums.HintStatus;
using static ArchipelagoMultiTextClient.Scripts.MainController;

namespace ArchipelagoMultiTextClient.Scripts;

public partial class HintTable : RecyclingTable<HintRow, HintData>
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

        UpdateData(orderedHints.ToHashSet());
        RefreshUI = false;
    }

    public IOrderedEnumerable<HintData> Order(IOrderedEnumerable<HintData> arr, Func<HintData, int> compare,
        bool descending)
        => !descending ? arr.OrderBy(compare) : arr.OrderByDescending(compare);

    public int GetOrderSlot(int slot) => PlayerSlots.ContainsKey(slot) ? Players.Length + slot : slot;

    public int SortNumber(ItemFlags flags) => ItemToSortId.GetValueOrDefault(flags, 2);

    protected override HintRow CreateRow() => new();
}

public class HintRow : RowItem<HintData>
{
    private Button _CopyHint = new();
    private Label _ReceivingPlayer = new();
    private Label _Item = new();
    private Label _FindingPlayer = new();
    private Label _Priority = new();
    private Label _Location = new();
    private Label _Entrance = new();
    private string? _CopyText;

    public HintRow()
    {
        SetTheme(_CopyHint);
        SetTheme(_ReceivingPlayer);
        SetTheme(_Item);
        SetTheme(_FindingPlayer);
        SetTheme(_Priority);
        SetTheme(_Location);
        SetTheme(_Entrance);
        _CopyHint.Pressed += () =>
        {
            if (_CopyText is null) return;
            DisplayServer.ClipboardSet(_CopyText);
        };
        _CopyHint.Text = "Copy";
    }

    public override void RefreshData(HintData data)
    {
        _ReceivingPlayer.Text = data.ReceivingPlayer;
        _ReceivingPlayer.Modulate = PlayerColor(data.ReceivingPlayer);
        _Item.Text = data.Item;
        _Item.Modulate =
            MainController.Data.ColorSettings[ItemToColorId.GetValueOrDefault(data.ItemFlags, "item_normal")];
        _FindingPlayer.Text = data.FindingPlayer;
        _FindingPlayer.Modulate = PlayerColor(data.FindingPlayer);
        _Priority.Text = HintStatusText[data.HintStatus];
        _Priority.Modulate = MainController.Data.ColorSettings[HintStatusColor[data.HintStatus]];
        _Location.Text = data.Location;
        _Location.Modulate = MainController.Data.ColorSettings["location"];
        _Entrance.Text = data.Entrance;
        _Entrance.Modulate =
            MainController.Data.ColorSettings[data.Entrance == "Vanilla" ? "entrance_vanilla" : "entrance"];
        _CopyText = data.GetCopyText();
    }

    public override void SetVisibility(bool isVisible)
    {
        _CopyText = null;
        _CopyHint.Visible = isVisible;
        _ReceivingPlayer.Visible = isVisible;
        _ReceivingPlayer.HorizontalAlignment = HorizontalAlignment.Center;
        _Item.Visible = isVisible;
        _Item.HorizontalAlignment = HorizontalAlignment.Center;
        _FindingPlayer.Visible = isVisible;
        _FindingPlayer.HorizontalAlignment = HorizontalAlignment.Center;
        _Priority.Visible = isVisible;
        _Priority.HorizontalAlignment = HorizontalAlignment.Center;
        _Location.Visible = isVisible;
        _Entrance.Visible = isVisible;
    }

    public override void SetParent(GridContainer toParent)
    {
        toParent.AddChild(_CopyHint);
        toParent.AddChild(_ReceivingPlayer);
        toParent.AddChild(_Item);
        toParent.AddChild(_FindingPlayer);
        toParent.AddChild(_Priority);
        toParent.AddChild(_Location);
        toParent.AddChild(_Entrance);
    }

    public void SetTheme(Control control)
    {
        control.AddThemeFontSizeOverride("font_size", 18);
        control.AddThemeFontOverride("font", MainController.Font);
    }
}

public struct HintData(Hint hint)
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
    private string? _CopyText = null;

    public string GetCopyText()
    {
        if (_CopyText is not null) return _CopyText;
        return _CopyText =
            $"`{ReceivingPlayer}`'s __{Item}__ is in `{FindingPlayer}`'s world at **{Location}**\n-# {Entrance}";
    }
}