using System.Collections.Generic;
using System.Linq;
using Archipelago.MultiClient.Net.Models;
using ArchipelagoMultiTextClient.Scripts.HintTab;
using ArchipelagoMultiTextClient.Scripts.TextClientTab;
using static ArchipelagoMultiTextClient.Scripts.MainController;
using static ArchipelagoMultiTextClient.Scripts.SettingsTab.Settings;

namespace ArchipelagoMultiTextClient.Scripts.UtilitiesTab;

public partial class Inventory : TextTable
{
    public static bool RefreshUI = true;
    private static Dictionary<string, List<ItemInfo>> Items = [];

    public override void _Ready() { TextClient.SelectedClientChangedEvent += _ => RefreshUI = true; }

    public override void _Process(double delta)
    {
        if (!RefreshUI) return;
        if (ChosenTextClient is null) return;
        UpdateData(Items[ChosenTextClient.PlayerName]
                  .GroupBy(info => info.ItemName)
                  .OrderBy(group => HintTable.SortNumber(group.First().Flags))
                  .ThenByDescending(group => group.Count())
                  .Select(infoGrouping =>
                   {
                       var item = infoGrouping.First();
                       var metaString = ItemFilterDialog.GetMetaString(item);
                       var itemColor = GetItemHexColor(item.Flags, metaString);
                       var itemBgColor = GetItemHexBgColor(item.Flags, metaString);

                       return (string[])
                       [
                           $"{infoGrouping.Count():###,###}",
                           $"[bgcolor={itemBgColor}][color={itemColor}]{item.ItemName.Clean()}[/color][/bgcolor]"
                       ];
                   })
                  .ToList());
        RefreshUI = false;
    }

    public static void AddItems(string name, ItemInfo[] items)
    {
        if (!Items.ContainsKey(name)) Items[name] = [];
        Items[name].AddRange(items);
        RefreshUI = true;
    }

    public static void ClearItems(string name)
    {
        Items.Remove(name);
        RefreshUI = true;
    }
}