using Archipelago.MultiClient.Net.Models;
using ArchipelagoMultiTextClient.Scripts.UtilitiesTab;
using Godot;

namespace ArchipelagoMultiTextClient.Scripts.Extra;

public partial class ItemWindow : AcceptWindow
{
    [Export] private ItemTable _ItemTable;

    public void SetAndShowItems(string title, ItemInfo[] items)
    {
        SetAndShow(title, "");
        _ItemTable.UpdateList(items);
    }
    
    public void SetAndShowItemsForSpecificItem(string title, ItemInfo[] items, ItemInfo info)
    {
        SetAndShow(title, "");
        _ItemTable.UpdateListItemSpecific(items, info);
    }
}