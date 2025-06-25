using Archipelago.MultiClient.Net.Enums;
using Godot;

namespace ArchipelagoMultiTextClient.Scripts.Settings;

public partial class ItemFilterDialog : ConfirmationDialog
{
    private string _ItemName;
    private string _GameName;
    private long _ItemId;
    private ItemFlags _ItemFlags;

    public override void _Ready()
    {
        Confirmed += () =>
        {
            var filter = new ItemFilter(_ItemId, _ItemName, _GameName, _ItemFlags);
            MainController.Data.ItemFilters.Add(filter.UidCode, filter);
            Tables.ItemFilterer.RefreshUI = true;
        };
    }

    public void SetItem(string itemName, string gameName, long itemId, ItemFlags flags)
    {
        _ItemName = itemName;
        _GameName = gameName;
        _ItemId = itemId;
        _ItemFlags = flags;
        DialogText = $"Add [{itemName}]\nfrom [{gameName}]\nto the Item Filter?";
        Show();
    }

    public string GetMetaString(string itemName, string gameName, long itemId, ItemFlags flags)
        => $"itemdialog{itemName}&-&{gameName}&-&{itemId}&-&{(int)flags}".Replace("\"", "'");

    public void SetItem(string meta)
    {
        var split = meta[10..].Split("&-&");
        SetItem(split[0], split[1], long.Parse(split[2]), (ItemFlags)int.Parse(split[3]));
    }
}