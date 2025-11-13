using System;
using System.Collections.Generic;
using System.Linq;
using Archipelago.MultiClient.Net.Enums;
using Godot;
using static Archipelago.MultiClient.Net.Enums.HintStatus;
using static Archipelago.MultiClient.Net.Enums.ItemFlags;
using static ArchipelagoMultiTextClient.Scripts.MainController;
using static ArchipelagoMultiTextClient.Scripts.SettingsTab.Settings;
using MultiworldName = ArchipelagoMultiTextClient.Scripts.LoginTab.MultiworldName;
using TextTable = ArchipelagoMultiTextClient.Scripts.Extra.TextTable;

namespace ArchipelagoMultiTextClient.Scripts.HintTab;

public partial class HintTable : TextTable
{
    public static bool RefreshUI;
    public static Dictionary<ItemFlags, int> ItemToSortIdCache = new();

    [Export] private CheckBox _ShowFound;
    [Export] private CheckBox _ShowPriority;
    [Export] private CheckBox _ShowUnspecified;
    [Export] private CheckBox _ShowNoPriority;
    [Export] private CheckBox _ShowAvoid;
    [Export] private PopupMenu _HintChangePopup;
    private string[] _CurrentItemSelected;

    public List<SortObject> SortOrder => Data.HintSortOrder;

    public override void _Ready()
    {
        _ShowFound.Pressed += () =>
        {
            Data.HintOptions[0] = _ShowFound.ButtonPressed;
            RefreshUI = true;
        };
        _ShowPriority.Pressed += () =>
        {
            Data.HintOptions[1] = _ShowPriority.ButtonPressed;
            RefreshUI = true;
        };
        _ShowUnspecified.Pressed += () =>
        {
            Data.HintOptions[2] = _ShowUnspecified.ButtonPressed;
            RefreshUI = true;
        };
        _ShowNoPriority.Pressed += () =>
        {
            Data.HintOptions[3] = _ShowNoPriority.ButtonPressed;
            RefreshUI = true;
        };
        _ShowAvoid.Pressed += () =>
        {
            Data.HintOptions[4] = _ShowAvoid.ButtonPressed;
            RefreshUI = true;
        };
        _ShowFound.ButtonPressed = Data.HintOptions[0];
        _ShowPriority.ButtonPressed = Data.HintOptions[1];
        _ShowUnspecified.ButtonPressed = Data.HintOptions[2];
        _ShowNoPriority.ButtonPressed = Data.HintOptions[3];
        _ShowAvoid.ButtonPressed = Data.HintOptions[4];

        MetaClicked += raw =>
        {
            var s = (string)raw;
            if (s.StartsWith("itemdialog"))
            {
                SetAndShowItemFilterDialogue(s);
                return;
            }

            if (s.StartsWith("SortOrder&"))
            {
                s = s[10..];
                if (SortOrder.Any(so => so.Name == s))
                {
                    var so = SortOrder.First(so => so.Name == s);
                    if (so.IsDescending)
                    {
                        SortOrder.Remove(so);
                    }
                    else
                    {
                        so.IsDescending = true;
                    }
                }
                else
                {
                    SortOrder.Add(new SortObject(s));
                }

                RefreshUI = true;
            }
            else if (s.StartsWith("change&"))
            {
                _CurrentItemSelected = s[7..].Split("&_&");
                _HintChangePopup.Position = Vector2I.Zero;
                _HintChangePopup.Popup(new Rect2I((Vector2I)_HintChangePopup.GetMousePosition(),
                    _HintChangePopup.Size));
            }
            else
            {
                DisplayServer.ClipboardSet(s);
            }
        };

        _HintChangePopup.IndexPressed += l =>
        {
            var client = ActiveClients.First(client
                => client.PlayerName == PlayerSlots[int.Parse(_CurrentItemSelected[0])]);
            client.UpdateHint(int.Parse(_CurrentItemSelected[1]), long.Parse(_CurrentItemSelected[4]), l switch
            {
                0 => Priority,
                1 => NoPriority,
                2 => Avoid
            });
        };
    }

    public override void _Process(double delta)
    {
        if (!RefreshUI) return;

        var orderedHints =
            MultiworldName.CurrentWorld.HintDatas.Values
                          .Where(hint =>
                           {
                               var order1 = GetOrderSlot(hint.FindingPlayerSlot);
                               return !(GetOrderSlot(hint.ReceivingPlayerSlot) == order1 && order1 == 1);
                           })
                          .Where(hint => hint.HintStatus switch
                           {
                               Found => _ShowFound.ButtonPressed,
                               Unspecified => _ShowUnspecified.ButtonPressed,
                               NoPriority => _ShowNoPriority.ButtonPressed,
                               Avoid => _ShowAvoid.ButtonPressed,
                               Priority => _ShowPriority.ButtonPressed,
                               _ => false
                           })
                          .Where(hint => !Data.ItemFilters.TryGetValue(hint.ItemUid, out var filter) ||
                                         filter.ShowInHintsTable)
                          .OrderBy(hint => hint.LocationId);

        if (SortOrder.Count > 0)
        {
            orderedHints = SortingOrder(orderedHints, SortOrder[0], true);
        }

        if (SortOrder.Count > 1)
        {
            orderedHints = SortOrder.Skip(1)
                                    .Aggregate(orderedHints, (current, option)
                                         => SortingOrder(current, option));
        }

        UpdateData(orderedHints.Select(hint => hint.GetData()).ToList());
        RefreshUI = false;
        return;

        IOrderedEnumerable<HintData> SortingOrder(IOrderedEnumerable<HintData> current, SortObject option,
            bool isFirst = false)
        {
            return option.Name switch
            {
                "Receiving Player" => Order(current, hint => GetOrderSlot(hint.ReceivingPlayerSlot),
                    option.IsDescending, isFirst),
                "Item" => Order(current, hint => SortNumber(hint.ItemFlags), option.IsDescending, isFirst),
                "Finding Player" => Order(current, hint => GetOrderSlot(hint.FindingPlayerSlot), option.IsDescending,
                    isFirst),
                "Priority" => Order(current, hint => HintStatusNumber[hint.HintStatus], option.IsDescending, isFirst),
            };
        }
    }

    public override string GetColumnText(string columnText, int columnNum)
    {
        if (columnNum is 0 or > 4) return columnText;

        if (SortOrder.Any(so => so.Name == columnText))
        {
            var so = SortOrder.First(so => so.Name == columnText);
            var place = SortOrder.IndexOf(so) + 1;
            return so.IsDescending
                ? $"[url=\"SortOrder&{columnText}\"]{columnText} {place}▼[/url]" // ↓▼▽v
                : $"[url=\"SortOrder&{columnText}\"]{columnText} {place}▲[/url]"; // ↑▲△^
        }

        return $"[url=\"SortOrder&{columnText}\"]{columnText} -[/url]";
    }

    public IOrderedEnumerable<HintData> Order(IOrderedEnumerable<HintData> arr, Func<HintData, int> compare,
        bool descending, bool first)
    {
        if (first) return !descending ? arr.OrderBy(compare) : arr.OrderByDescending(compare);
        return !descending ? arr.ThenBy(compare) : arr.ThenByDescending(compare);
    }

    public int GetOrderSlot(int slot)
    {
        if (ActiveClients.Any(client => client.PlayerSlot == slot)) return 3;
        return Main.HasSlotName(ActiveClients[0].PlayerNames[slot]) ? 2 : 1;
    }

    public static int SortNumber(ItemFlags flags)
    {
        if (ItemToSortIdCache.TryGetValue(flags, out var id)) return id;
        if ((flags & Advancement) == Advancement)
        {
            id = 0;
        }
        else if ((flags & NeverExclude) == NeverExclude)
        {
            id = 1;
        }
        else if ((flags & Trap) == Trap)
        {
            id = 10;
        }
        else
        {
            id = 2;
        }

        return ItemToSortIdCache[flags] = id;
    }
}