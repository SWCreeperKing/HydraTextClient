using System.Collections.Generic;
using System.Linq;
using Archipelago.MultiClient.Net.Models;
using ArchipelagoMultiTextClient.Scripts.HintTab;
using ArchipelagoMultiTextClient.Scripts.TextClientTab;
using Godot;
using static ArchipelagoMultiTextClient.Scripts.MainController;
using static ArchipelagoMultiTextClient.Scripts.SettingsTab.Settings;

namespace ArchipelagoMultiTextClient.Scripts.UtilitiesTab;

public partial class Inventory : TextTable
{
    public bool RefreshUI = true;
    public List<ItemInfo> Items = [];

    public override void _Ready()
    {
        BbcodeEnabled = true;
        AutowrapMode = TextServer.AutowrapMode.Off;
        FitContent = true;
        TextClient.SelectedClientChangedEvent += _ => RefreshUI = true;
    }

    public override void _Process(double delta)
    {
        if (!RefreshUI) return;
        if (ChosenTextClient is null) return;
        UpdateData(Items.GroupBy(info => info.ItemName)
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

    public void AddItems(ItemInfo[] items, bool firstSend)
    {
        Items.AddRange(items);
        RefreshUI = true;
    }
}