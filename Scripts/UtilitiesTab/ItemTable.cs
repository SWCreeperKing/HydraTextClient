using System.Linq;
using Archipelago.MultiClient.Net.Models;
using ArchipelagoMultiTextClient.Scripts.HintTab;
using static ArchipelagoMultiTextClient.Scripts.MainController;
using TextTable = ArchipelagoMultiTextClient.Scripts.Extra.TextTable;

namespace ArchipelagoMultiTextClient.Scripts.UtilitiesTab;

public partial class ItemTable : TextTable
{
    public void UpdateList(ItemInfo[] items)
    {
        _Columns = ["Count", "Item"];
        if (ChosenTextClient is null) return;
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
        _Columns = ["Count", "Player"];
        if (ChosenTextClient is null) return;
        UpdateData(items.Where(item => item.ItemId == specificItem.ItemId)
                        .GroupBy(info => info.Player.Name)
                        .OrderBy(group => HintTable.SortNumber(group.First().Flags))
                        .ThenByDescending(group => group.Count())
                        .Select(infoGrouping =>
                         {
                             var item = infoGrouping.First();
                             return (string[])
                             [
                                 $"{infoGrouping.Count():###,###}",
                                 $"[color={PlayerColor(item.Player.Slot).Hex}]{GetAlias(item.Player.Slot, true)}[/color]"
                             ];
                         })
                        .ToList());
    }
}