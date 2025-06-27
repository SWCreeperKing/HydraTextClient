using System;
using System.Collections.Generic;
using System.Linq;
using Archipelago.MultiClient.Net.Enums;
using ArchipelagoMultiTextClient.Scripts.Login;
using ArchipelagoMultiTextClient.Scripts.TextClientTab;
using Godot;
using static Archipelago.MultiClient.Net.Enums.HintStatus;
using static Archipelago.MultiClient.Net.Enums.ItemFlags;
using static ArchipelagoMultiTextClient.Scripts.MainController;
using static ArchipelagoMultiTextClient.Scripts.Settings.Settings;

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
    [Export] private HintChangerWindow _HintChangerWindow;

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
                ItemFilterDialog.SetItem(s);
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
                var split = s[7..].Split("&_&");
                _HintChangerWindow.ShowWindow(int.Parse(split[1]), split[0], split[2], split[3], long.Parse(split[4]));
            }
            else
            {
                DisplayServer.ClipboardSet(s);
            }
        };
    }

    public override void _Process(double delta)
    {
        if (!RefreshUI) return;

        var orderedHints =
            // MultiworldName.CurrentWorld.HintDatas.Values.Where(hint => hint.HintStatus switch
            MultiworldName.Datas.Where(hint => hint.HintStatus switch
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

        orderedHints = SortOrder.Aggregate(orderedHints, (current, option) => option.Name switch
        {
            "Receiving Player" => Order(current, hint => GetOrderSlot(hint.ReceivingPlayerSlot), option.IsDescending),
            "Item" => Order(current, hint => SortNumber(hint.ItemFlags), option.IsDescending),
            "Finding Player" => Order(current, hint => GetOrderSlot(hint.FindingPlayerSlot), option.IsDescending),
            "Priority" => Order(current, hint => HintStatusNumber[hint.HintStatus], option.IsDescending),
            _ => current
        });

        UpdateData(orderedHints.Select(hint => hint.GetData()).ToList());
        RefreshUI = false;
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
        bool descending)
        => !descending ? arr.OrderBy(compare) : arr.OrderByDescending(compare);

    public int GetOrderSlot(int slot) => PlayerSlots.ContainsKey(slot) ? Players.Length + slot : slot;

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