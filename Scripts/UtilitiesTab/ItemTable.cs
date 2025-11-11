using System.Collections.Generic;
using System.Linq;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using ArchipelagoMultiTextClient.Scripts.HintTab;
using static ArchipelagoMultiTextClient.Scripts.MainController;
using TextTable = ArchipelagoMultiTextClient.Scripts.Extra.TextTable;

namespace ArchipelagoMultiTextClient.Scripts.UtilitiesTab;

public partial class ItemTable : TextTable
{
    public void UpdateList(ItemInfo[] items)
    {
        if (ChosenTextClient is null) return;
        _Columns = ["Count", "Item"];
        UpdateData(items.GroupBy(info => info.ItemName)
                        .OrderBy(group => HintTable.SortNumber(group.First().Flags))
                        .ThenByDescending(group => group.Count())
                        .Select(infoGrouping => (string[])
                         [
                             $"{infoGrouping.Count():###,###}",
                             FormatItemColor(infoGrouping.First(), false)
                         ])
                        .ToList());
    }

    public void UpdateListItemSpecific(ItemInfo[] items, ItemInfo specificItem)
    {
        if (ChosenTextClient is null) return;
        _Columns = ["Count", "Player", "Locations"];
        UpdateData(items.Where(item => item.ItemId == specificItem.ItemId)
                        .GroupBy(info => info.Player.Name)
                        .OrderBy(group => HintTable.SortNumber(group.First().Flags))
                        .ThenByDescending(group => group.Count())
                        .Select(infoGrouping =>
                         {
                             var item = infoGrouping.First();
                             var locations = infoGrouping
                                            .Select(info
                                                 => $"[color={Data["location"].Hex}]{info.LocationName}[/color]")
                                            .ToArray();
                             var from = item.LocationName == "Cheat Console" ? 0 : item.Player.Slot;
                             return (string[])
                             [
                                 $"{infoGrouping.Count():###,###}",
                                 $"[color={PlayerColor(from).Hex}]{GetAlias(from, true)}[/color]",
                                 locations.Length <= 10 ? string.Join(",\n ", locations) : "Various Locations"
                             ];
                         })
                        .ToList());
    }

    public void UpdateListItemHistory(ItemInfo[] items)
    {
        if (ChosenTextClient is null) return;
        _Columns = ["Received Order", "Item", "From", "Location"];
        var itemHistoryRaw = items
                            .Select((item, index)
                                 => new ItemEntry(index + 1, -1,
                                     [FormatItemColor(item, false)],
                                     item.Flags,
                                     item.LocationName == "Cheat Console" ? 0 : item.Player.Slot,
                                     [$"[color={Data["location"].Hex}]{item.LocationName}[/color]"]
                                 ))
                            .ToArray();

        if (itemHistoryRaw.Length == 0) return;
        List<ItemEntry> itemHistory = [itemHistoryRaw[0]];

        for (var index = 1; index < itemHistoryRaw.Length; index++)
        {
            var current = itemHistoryRaw[index];
            var last = itemHistory[^1];

            if (last.Flags is not (ItemFlags.None or ItemFlags.Trap) ||
                current.Flags is not (ItemFlags.None or ItemFlags.Trap) || last.From != current.From)
            {
                itemHistory.Add(current);
                continue;
            }

            itemHistory[^1] = new ItemEntry(last.IndexStart, current.IndexStart, [..last.Items, ..current.Items],
                ItemFlags.None, last.From, [..last.Locations, ..current.Locations]);
        }

        UpdateData(itemHistory.Select(item => item.GetData()).ToList());
    }

    public readonly struct ItemEntry(
        int indexStart,
        int indexEnd,
        HashSet<string> items,
        ItemFlags flags,
        int from,
        HashSet<string> locations)
    {
        public readonly int IndexStart = indexStart;
        public readonly int IndexEnd = indexEnd;
        public readonly HashSet<string> Items = items;
        public readonly ItemFlags Flags = flags;
        public readonly int From = from;
        public readonly HashSet<string> Locations = locations;

        public string[] GetData()
            =>
            [
                indexEnd == -1 ? $"{IndexStart:###,###}" : $"{IndexStart:###,###} - {IndexEnd:###,###}",
                string.Join(",\n ", Items),
                $"[color={PlayerColor(From).Hex}]{GetAlias(From, true)}[/color]",
                Locations.Count <= 10 ? string.Join(",\n ", Locations) : "Various Locations"
            ];
    }
}