using System.Collections.Generic;
using System.Linq;
using Archipelago.MultiClient.Net.Models;
using ArchipelagoMultiTextClient.Scripts.HintTab;
using ArchipelagoMultiTextClient.Scripts.TextClientTab;
using Godot;
using static ArchipelagoMultiTextClient.Scripts.MainController;
using TextTable = ArchipelagoMultiTextClient.Scripts.Extra.TextTable;

namespace ArchipelagoMultiTextClient.Scripts.UtilitiesTab;

public partial class Inventory : TextTable
{
    public bool RefreshUI = true;
    public List<ItemInfo> Items = [];
    public Label CheatedLabel;

    public override void _Ready()
    {
        BbcodeEnabled = true;
        AutowrapMode = TextServer.AutowrapMode.Off;
        FitContent = true;
        TextClient.SelectedClientChangedEvent += _ => RefreshUI = true;

        MetaClicked += v =>
        {
            if (!long.TryParse((string)v, out var hashcode)) return;
            var item = Items.Find(item => item.GetHashCode() == hashcode);
            InventoryManager.ItemWindow.SetAndShowItemsForSpecificItem($"Showing Details for item: [{item.ItemName}]", Items.ToArray(), item);
        };
    }

    public override void _Process(double delta)
    {
        if (!RefreshUI) return;
        if (ChosenTextClient is null) return;
        UpdateData(Items.GroupBy(info => info.ItemName)
                        .OrderBy(group => HintTable.SortNumber(group.First().Flags))
                        .ThenByDescending(group => group.Count())
                        .Select(infoGrouping => (string[])
                         [
                             $"{infoGrouping.Count():###,###}",
                             $"[url=\"{infoGrouping.First().GetHashCode()}\"]{FormatItemColor(infoGrouping.First(), false)}[/url]"
                         ])
                        .ToList());
        var cheatedItems = Items.Count(item => item.LocationName == "Cheat Console");
        CheatedLabel.Text = cheatedItems == 0 ? "" : $"Cheated Items: [{cheatedItems:###,###}]"; 
        RefreshUI = false;
    }

    public void AddItems(ItemInfo[] items)
    {
        Items.AddRange(items);
        RefreshUI = true;
    }
}